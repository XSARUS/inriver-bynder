using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                if (states.Count == 0)
                {
                    return;
                }

                Context.Log(LogLevel.Information, $"Start handling of {states.Count} Bynder Notifications");

                var notificationWorker = Container.GetInstance<NotificationWorker>();
                int updatedWorkerCalledCount = 0;
                int maxUpdatedWorkerCalledCount = SettingHelper.GetMaxUpdatedWorkerCalledCount(Context.Settings, Context.Logger);
                int retried = 0;
                int failed = 0;
                int succesful = 0;
                int deleted = 0;
                int maxRetryAttempts = SettingHelper.GetMaxRetryAttempts(Context.Settings, Context.Logger);

                var assetDeletedWorker = Container.GetInstance<AssetDeletedWorker>();
                var assetWorker = Container.GetInstance<AssetUpdatedWorker>();

                foreach (ConnectorState state in states.OrderBy(s => s.Created))
                {
                    var stateData = JsonConvert.DeserializeObject<AttemptSNSMessageWrapper>(state.Data);
                    var notificationMessage = stateData.OriginalMessageJson;
                    List<string> resultMessages = new List<string>(8);

                    try
                    {
                        Context.Log(LogLevel.Debug, $"Handling Bynder Notifications of ConnectorState {state.Id} created at {state.Created} attempt {stateData.Attempt}/{maxRetryAttempts}");

                        var notificationResult = notificationWorker.Execute(notificationMessage);
                        resultMessages = notificationResult.Messages;

                        if (!string.IsNullOrEmpty(notificationResult.MediaId))
                        {
                            WorkerResult workerResult;

                            if (notificationResult.NotificationType == NotificationType.IsDeleted)
                            {
                                workerResult = assetDeletedWorker.Execute(notificationResult.MediaId);
                                deleted++;
                            }
                            else
                            {
                                if (updatedWorkerCalledCount >= maxUpdatedWorkerCalledCount)
                                {
                                    continue;
                                }
                                workerResult = assetWorker.Execute(notificationResult.MediaId, notificationResult.NotificationType);
                                succesful++;
                            }

                            resultMessages.AddRange(workerResult.Messages);
                        }

                        Context.Log(LogLevel.Debug, $"Handled Bynder Notification of ConnectorState {state.Id} created at {state.Created} | Result-messages: {string.Join(Environment.NewLine, resultMessages)}");

                        Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                    }
                    catch (Exception e)
                    {
                        Context.Log(LogLevel.Error, $"Failed handling Bynder Notification of ConnectorState {state.Id} created at {state.Created} [attempt {stateData.Attempt}/{maxRetryAttempts}]: {e.Message} | Result-messages: {string.Join(Environment.NewLine, resultMessages)}", e);
                        Context.Log(LogLevel.Verbose, $"Failed for ConnectorState {state.Id} created at {state.Created} with data: {state.Data}");

                        // Check for 429 Too Many Requests
                        if (ExceptionHelper.IsTooManyRequestsException(e))
                        {
                            // Rethrow to interrupt the foreach and let the outer try/catch handle it
                            Context.Log(LogLevel.Error, $"Too many request (rate-limit)! [ConnectorState {state.Id} was being handled]: {e.Message}", e);
                            throw;
                        }

                        if (ExceptionHelper.Is500ServerErrorException(e))
                        {
                            // Rethrow to interrupt the foreach and let the outer try/catch handle it
                            Context.Log(LogLevel.Error, $"A server error! [ConnectorState {state.Id} was being handled]: {e.Message}", e);
                            throw;
                        }

                        if (stateData.Attempt < maxRetryAttempts)
                        {
                            stateData.Attempt++;
                            state.Data = JsonConvert.SerializeObject(stateData);
                            var updatedState = Context.ExtensionManager.UtilityService.UpdateConnectorState(state);
                            retried++;
                        }
                        else
                        {
                            Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                            Context.Log(LogLevel.Error, $"Max retry attempts reached for ConnectorState {state.Id}", e);
                            failed++;
                        }
                    }
                }

                assetWorker.ResetMetaProperties();
                Context.Log(LogLevel.Information, $"Finished handling of {states.Count} Bynder Notifications [{succesful} created/updated | {deleted} deleted | {failed} failed | {retried} retried]");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods
    }
}