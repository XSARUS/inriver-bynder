using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bynder.Extension
{
    using Api;
    using Config;
    using Enums;
    using Models;
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

                settings.Add(Settings.MaxRetryAttempts, Settings.DefaultMaxRetryAttempts.ToString());
                settings.Add(Settings.MaxUpdatesToHandle, Settings.DefaultMaxUpdatesToHandle.ToString());

                return settings;
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion Properties

        #region Methods

        public override string Test()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.Test() ?? string.Empty);

            try
            {
                List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(Names.ConnectorStateIds.BynderNotificationListener);
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
                List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(Names.ConnectorStateIds.BynderNotificationListener);
                int numOfStates = states.Count;
                if (numOfStates == 0)
                {
                    return;
                }

                Context.Log(LogLevel.Information, $"Start handling of {numOfStates} Bynder Notifications");

                var notificationWorker = Container.GetInstance<NotificationWorker>();
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

                var tasks = states
                    .OrderBy(s => s.Created)
                    .Select(state => Task.Run(async () =>
                    {
                        token.ThrowIfCancellationRequested();

                        await semaphore.WaitAsync(token);

                        try
                        {
                            token.ThrowIfCancellationRequested();

                            var stateData = JsonSerializer.Deserialize<AttemptSNSMessageWrapper>(state.Data, JsonOptions);
                            var notificationMessage = stateData.OriginalMessageJson;
                            var resultMessages = new List<string>(8);

                            Context.Log(LogLevel.Debug,
                                $"Handling ConnectorState {state.Id} attempt {stateData.Attempt}/{maxRetryAttempts}");

                            var notificationResult = notificationWorker.Execute(notificationMessage);
                            resultMessages = notificationResult.Messages;

                            if (!string.IsNullOrEmpty(notificationResult.MediaId))
                            {
                                WorkerResult workerResult;

                                if (notificationResult.NotificationType == NotificationType.IsDeleted)
                                {
                                    workerResult = assetDeletedWorker.Execute(notificationResult.MediaId);
                                    Interlocked.Increment(ref deleted);
                                }
                                else
                                {
                                    if (Interlocked.Increment(ref updatedWorkerCalledCount) > maxUpdatedWorkerCalledCount)
                                        return;

                                    workerResult = assetWorker.Execute(
                                        notificationResult.MediaId,
                                        notificationResult.NotificationType);

                                    Interlocked.Increment(ref successful);
                                }

                                resultMessages.AddRange(workerResult.Messages);
                            }

                            Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                        }
                        catch (Exception e)
                        {
                            var stateData = JsonSerializer.Deserialize<AttemptSNSMessageWrapper>(state.Data, JsonOptions);

                            Context.Log(LogLevel.Error,
                                $"Failed handling ConnectorState {state.Id} [attempt {stateData.Attempt}/{maxRetryAttempts}]: {e.Message}",
                                e);

                            Context.Log(LogLevel.Verbose,
                                $"Failed for ConnectorState {state.Id} with data: {state.Data}");

                            if (ExceptionHelper.IsTooManyRequestsException(e) ||
                                ExceptionHelper.Is500ServerErrorException(e))
                            {
                                Context.Log(LogLevel.Error,
                                    $"Critical API issue → stopping entire batch. State {state.Id}: {e.Message}", e);

                                cts.Cancel(); // Stopt alle andere tasks ASAP

                                throw;
                            }

                            // normale retry flow
                            if (stateData.Attempt < maxRetryAttempts)
                            {
                                stateData.Attempt++;
                                state.Data = JsonSerializer.Serialize(stateData, JsonOptions);
                                Context.ExtensionManager.UtilityService.UpdateConnectorState(state);
                                Interlocked.Increment(ref retried);
                            }
                            else
                            {
                                Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                                Context.Log(LogLevel.Error,
                                    $"Max retry attempts reached for ConnectorState {state.Id}", e);

                                Interlocked.Increment(ref failed);
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