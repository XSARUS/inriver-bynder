using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Extension
{
    using Amazon.SimpleNotificationService.Util;
    using Bynder.Config;
    using Bynder.Models;
    using Bynder.Utils.Helpers;
    using Enums;
    using Workers;

    public class ScheduledNotificationHandler : AbstractScheduledExtension
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = base.DefaultSettings;
                settings.Add(Settings.MaxRetryAttempts, Settings.DefaultMaxRetryAttempts.ToString());
                return settings;
            }
        }

        #endregion Properties

        #region Methods

        protected override void Execute()
        {
            try
            {
                Context.Log(LogLevel.Information, "Start handling of Bynder Notifications");

                List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(Names.ConnectorStateIds.BynderNotificationListener);
                if (states.Count == 0)
                {
                    Context.Log(LogLevel.Information, "Finished handling of 0 Bynder Notifications");
                    return;
                }

                var notificationWorker = Container.GetInstance<NotificationWorker>();
                int retried = 0;
                int failed = 0;
                int succesful = 0;
                int maxRetryAttempts = SettingHelper.GetMaxRetryAttempts(DefaultSettings, Context.Logger);

                foreach (ConnectorState state in states.OrderBy(s => s.Created))
                {
                    string result = string.Empty;

                    var stateData = JsonConvert.DeserializeObject<AttemptSNSMessageWrapper>(state.Data);
                    var notificationMessage = stateData.OriginalMessageJson;

                    try
                    {
                        Context.Log(LogLevel.Debug, $"Handling Bynder Notifications of ConnectorState {state.Id} created at {state.Created} attempt {stateData.Attempt}/{maxRetryAttempts}");

                        var notificationResult = notificationWorker.Execute(notificationMessage);
                        var resultMessages = notificationResult.Messages;

                        if (!string.IsNullOrEmpty(notificationResult.MediaId))
                        {
                            WorkerResult workerResult;

                            if (notificationResult.NotificationType == NotificationType.IsDeleted)
                            {
                                var assetDeletedWorker = Container.GetInstance<AssetDeletedWorker>();
                                workerResult = assetDeletedWorker.Execute(notificationResult.MediaId);
                            }
                            else
                            {
                                var assetWorker = Container.GetInstance<AssetUpdatedWorker>();
                                workerResult = assetWorker.Execute(notificationResult.MediaId, notificationResult.NotificationType);
                            }

                            resultMessages.AddRange(workerResult.Messages);
                        }

                        result = string.Join(Environment.NewLine, resultMessages);

                        Context.Log(LogLevel.Verbose, $"Result for ConnectorState {state.Id}: {result}");
                        Context.Log(LogLevel.Debug, $"Handled Bynder Notifications of ConnectorState {state.Id} created at {state.Created}");
                        succesful++;

                        Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                    }
                    catch (Exception e)
                    {
                        Context.Log(LogLevel.Error, $"Failed handling Bynder Notifications of ConnectorState {state.Id} created at {state.Created} [attempt {stateData.Attempt}/{maxRetryAttempts}]: {e.Message}", e);
                        Context.Log(LogLevel.Verbose, state.Data);

                        if (stateData.Attempt < maxRetryAttempts)
                        {
                            stateData.Attempt++;
                            state.Data = JsonConvert.SerializeObject(stateData);
                            var updatedState = Context.ExtensionManager.UtilityService.UpdateConnectorState(state);
                            retried++;
                            Context.Log(LogLevel.Debug, $"ConnectorState {state.Id} updated {updatedState.Modified} for next attempt {stateData.Attempt}/{maxRetryAttempts}");
                        }
                        else
                        {
                            Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                            failed++;
                            Context.Log(LogLevel.Error, $"Max retry attempts reached for ConnectorState {state.Id}", e);
                        }
                    }
                }

                Context.Log(LogLevel.Information, $"Finished handling of {states.Count} Bynder Notifications [{succesful} successfull | {failed} failed | {retried} retried]");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods
    }
}