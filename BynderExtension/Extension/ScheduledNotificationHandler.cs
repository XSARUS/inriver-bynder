using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bynder.Extension
{
    using Api;
    using Config;
    using Enums;
    using Models;
    using Names;
    using SettingProviders;
    using Utils.Helpers;
    using Workers;

    public class ScheduledNotificationHandler : AbstractScheduledExtension
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = base.DefaultSettings;

                foreach (var setting in SettingNames.GetDefaultBynderApiSettings())
                {
                    settings[setting.Key] = setting.Value;
                }

                foreach (var setting in NotificationWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                foreach (var setting in AssetUpdatedWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                foreach (var setting in AssetDeletedWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                settings.Add(Settings.ConnectorStateName, ConnectorStateIds.BynderNotificationListener);
                settings.Add(Settings.MaxRetryAttempts, Settings.DefaultMaxRetryAttempts.ToString());
                settings.Add(Settings.MaxUpdatesToHandle, Settings.DefaultMaxUpdatesToHandle.ToString());

                return settings;
            }
        }

        #endregion Properties

        #region Methods

        public override string Test()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.Test() ?? string.Empty);

            try
            {
                var connectorStateName = SettingHelper.GetConnectorStateName(Context.Settings, Context.Logger, ConnectorStateIds.BynderNotificationListener);
                List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(connectorStateName);
                sb.AppendLine($"Number of connectorstates found: {states.Count}");
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }

        protected override void Execute()
        {
            try
            {
                var connectorStateName = SettingHelper.GetConnectorStateName(Context.Settings, Context.Logger, ConnectorStateIds.BynderNotificationListener);
                List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(connectorStateName);
                if (states.Count == 0)
                {
                    return;
                }

                Context.Log(LogLevel.Information, $"Start handling of {states.Count} Bynder Notifications");

                var notificationWorker = Container.GetInstance<NotificationWorker>();

                // gather notifications for later processing
                var notifications = states
                    .AsParallel()
                    .Select(state =>
                    {
                        try
                        {
                            var stateData = JsonConvert.DeserializeObject<AttemptSNSMessageWrapper>(state.Data);

                            var notificationResult =
                                notificationWorker.Execute(stateData.OriginalMessageJson);

                            if (string.IsNullOrWhiteSpace(notificationResult.MediaId))
                            {
                                Context.Log(LogLevel.Warning, $"ConnectorState {state.Id} has no MediaId and will be deleted");
                                Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                                return null;
                            }

                            return new NotificationContext
                            {
                                State = state,
                                StateData = stateData,
                                Result = notificationResult
                            };
                        }
                        catch (Exception ex)
                        {
                            Context.Log(
                                LogLevel.Error,
                                $"Failed parsing ConnectorState {state.Id}",
                                ex);

                            Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);

                            return null;
                        }
                    })
                    .Where(x => x != null)
                    .OrderBy(s => s.State.Created)
                    .ToList();

                // Deduplicate on MediaId + NotificationType
                // Keep latest, remove older duplicates
                var notificationsToProcess = new List<NotificationContext>();
                var statesToDelete = new List<int>();

                foreach (var group in notifications.GroupBy(x => new
                {
                    x.Result.MediaId,
                    x.Result.NotificationType 
                }))
                {

                    // take latest notification for each mediaId + notificationType combination
                    var latest = group.Last();

                    notificationsToProcess.Add(latest);

                    statesToDelete.AddRange(
                        group
                            .Where(x => x.State.Id != latest.State.Id)
                            .Select(x => x.State.Id));
                }

                if (statesToDelete.Any())
                {
                    Context.Log(
                        LogLevel.Information,
                        $"Deleting {statesToDelete.Count} duplicate ConnectorStates");

                    Context.ExtensionManager.UtilityService.DeleteConnectorStates(statesToDelete);
                }

                // process by media id so we don't parallel process the same media id at the same time
                var mediaGroups = notificationsToProcess
                    .GroupBy(x => x.Result.MediaId)
                    .ToList();

                Context.Log(
                    LogLevel.Information,
                    $"Processing {mediaGroups.Count} unique MediaIds");

                int updatedWorkerCalledCount = 0;
                int maxUpdatedWorkerCalledCount = SettingHelper.GetMaxUpdatedWorkerCalledCount(Context.Settings, Context.Logger);

                int retried = 0;
                int failed = 0;
                int successful = 0;
                int deleted = 0;

                int maxRetryAttempts = SettingHelper.GetMaxRetryAttempts(Context.Settings, Context.Logger);

                var assetDeletedWorker = Container.GetInstance<AssetDeletedWorker>();
                var assetWorker = Container.GetInstance<AssetUpdatedWorker>();

                var semaphore = new SemaphoreSlim(3);
                var cts = new CancellationTokenSource();
                var token = cts.Token;

                var tasks = mediaGroups
                    .Select(group => Task.Run(async () =>
                    {
                        token.ThrowIfCancellationRequested();

                        await semaphore.WaitAsync(token);

                        try
                        {
                            foreach (var notification in group)
                            {
                                try
                                {
                                    token.ThrowIfCancellationRequested();

                                    Context.Log(
                                        LogLevel.Debug,
                                        $"Handling ConnectorState {notification.State.Id} attempt {notification.StateData.Attempt}/{maxRetryAttempts}");

                                    var resultMessages = notification.Result.Messages;

                                    if (notification.Result.NotificationType == NotificationType.IsDeleted)
                                    {
                                        var workerResult =
                                            assetDeletedWorker.Execute(notification.Result.MediaId);

                                        resultMessages.AddRange(workerResult.Messages);

                                        Interlocked.Increment(ref deleted);
                                    }
                                    else
                                    {
                                        if (Interlocked.Increment(ref updatedWorkerCalledCount)
                                            > maxUpdatedWorkerCalledCount)
                                        {
                                            break;
                                        }

                                        var workerResult = assetWorker.Execute(
                                            notification.Result.MediaId,
                                            notification.Result.NotificationType);

                                        resultMessages.AddRange(workerResult.Messages);

                                        Interlocked.Increment(ref successful);
                                    }

                                    Context.ExtensionManager.UtilityService.DeleteConnectorState(
                                        notification.State.Id);

                                    Context.Log(
                                        LogLevel.Debug,
                                        $"Handled ConnectorState {notification.State.Id} created at {notification.State.Created}");
                                }
                                catch (Exception e)
                                {
                                    Context.Log(
                                        LogLevel.Error,
                                        $"Failed handling ConnectorState {notification.State.Id} [attempt {notification.StateData.Attempt}/{maxRetryAttempts}]",
                                        e);

                                    if (ExceptionHelper.IsTooManyRequestsException(e) ||
                                        ExceptionHelper.Is500ServerErrorException(e))
                                    {
                                        Context.Log(
                                            LogLevel.Error,
                                            $"Critical API issue. Stopping batch. State {notification.State.Id}: {e.Message}",
                                            e);

                                        cts.Cancel();

                                        throw;
                                    }

                                    if (notification.StateData.Attempt < maxRetryAttempts)
                                    {
                                        notification.StateData.Attempt++;

                                        notification.State.Data = JsonConvert.SerializeObject(notification.StateData);

                                        Context.ExtensionManager.UtilityService
                                            .UpdateConnectorState(notification.State);

                                        Interlocked.Increment(ref retried);
                                    }
                                    else
                                    {
                                        Context.ExtensionManager.UtilityService
                                            .DeleteConnectorState(notification.State.Id);

                                        Context.Log(
                                            LogLevel.Error,
                                            $"Max retry attempts reached for ConnectorState {notification.State.Id}",
                                            e);

                                        Interlocked.Increment(ref failed);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, token))
                    .ToList();

                try
                {
                    Task.WaitAll(tasks.ToArray());
                }
                catch (AggregateException ae)
                {
                    var ex = ae.Flatten().InnerExceptions.FirstOrDefault();

                    if (ex != null)
                        throw;
                }
                cts.Dispose();

                assetWorker.ResetMetaProperties();
                Context.Log(LogLevel.Information, $"Finished handling of {states.Count} Bynder Notifications [{successful} created/updated | {deleted} deleted | {failed} failed | {retried} retried]");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods
    }
}