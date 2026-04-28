using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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

        #endregion Properties

        #region Methods

        public override string Test()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.Test() ?? string.Empty);

            try
            {
                List<ConnectorState> states = Context.ExtensionManager.UtilityService
                    .GetAllConnectorStatesForConnector(Names.ConnectorStateIds.BynderNotificationListener);

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
                var states = Context.ExtensionManager.UtilityService
                    .GetAllConnectorStatesForConnector(Names.ConnectorStateIds.BynderNotificationListener)
                    .OrderBy(s => s.Created);

                if (states.Count() == 0)
                    return;

                Context.Log(LogLevel.Information, $"Start handling of {states.Count()} Bynder Notifications");

                var notificationWorker = Container.GetInstance<NotificationWorker>();
                var assetDeletedWorker = Container.GetInstance<AssetDeletedWorker>();
                var assetWorker = Container.GetInstance<AssetUpdatedWorker>();

                int maxUpdatedWorkerCalledCount = SettingHelper.GetMaxUpdatedWorkerCalledCount(Context.Settings, Context.Logger);
                int maxRetryAttempts = SettingHelper.GetMaxRetryAttempts(Context.Settings, Context.Logger);

                // Thread-safe counters
                int updatedWorkerCalledCount = 0;
                int retried = 0;
                int failed = 0;
                int succesful = 0;
                int deleted = 0;

                // Throttling
                int maxDegreeOfParallelism = 4;
                var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

                var tasks = states
                    .Select(state => Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            var stateData = JsonConvert.DeserializeObject<AttemptSNSMessageWrapper>(state.Data);
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

                                    Interlocked.Increment(ref succesful);
                                }

                                resultMessages.AddRange(workerResult.Messages);
                            }

                            Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                var stateData = JsonConvert.DeserializeObject<AttemptSNSMessageWrapper>(state.Data);

                                if (stateData.Attempt < maxRetryAttempts)
                                {
                                    stateData.Attempt++;
                                    state.Data = JsonConvert.SerializeObject(stateData);
                                    Context.ExtensionManager.UtilityService.UpdateConnectorState(state);
                                    Interlocked.Increment(ref retried);
                                }
                                else
                                {
                                    Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                                    Interlocked.Increment(ref failed);
                                }
                            }
                            catch
                            {
                                Interlocked.Increment(ref failed);
                            }

                            Context.Log(LogLevel.Error,
                                $"Failed handling ConnectorState {state.Id}: {e.Message}", e);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }))
                    .ToList();

                Task.WaitAll(tasks.ToArray());

                assetWorker.ResetMetaProperties();

                Context.Log(LogLevel.Information,
                    $"Finished handling [{succesful} updated | {deleted} deleted | {failed} failed | {retried} retried]");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods
    }
}