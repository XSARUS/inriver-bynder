using Amazon.SimpleNotificationService.Util;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bynder.Extension
{
    using Bynder.Config;
    using Bynder.Models;
    using Bynder.Sdk.Model;
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

                // Remove settings that are not used in this extension:
                var settingsToRemove = new List<string>(12)
                {
                    Settings.BynderBrandName,
                    Settings.BynderLocaleForMetapropertyOptionLabel,
                    Settings.CvlMetapropertyMapping,
                    Settings.DownloadMediaType,
                    Settings.FilenameExtensionMediaTypeMapping,
                    Settings.ExportConditions,
                    Settings.InitialAssetLoadUrlQuery,
                    Settings.InitialAssetLoadLimit,
                    Settings.InRiverEntityUrl,
                    Settings.InRiverIntegrationId,
                    Settings.LocaleMappingInriverToBynder
                };

                settingsToRemove.ForEach(s => settings.Remove(s));

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
                int deleted = 0;
                int maxRetryAttempts = SettingHelper.GetMaxRetryAttempts(Context.Settings, Context.Logger);

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
                                deleted++;
                            }
                            else
                            {
                                var assetWorker = Container.GetInstance<AssetUpdatedWorker>();
                                workerResult = assetWorker.Execute(notificationResult.MediaId, notificationResult.NotificationType);
                                succesful++;
                            }

                            resultMessages.AddRange(workerResult.Messages);
                        }

                        result = string.Join(Environment.NewLine, resultMessages);

                        Context.Log(LogLevel.Verbose, $"Result for ConnectorState {state.Id}: {result}");
                        Context.Log(LogLevel.Debug, $"Handled Bynder Notifications of ConnectorState {state.Id} created at {state.Created}");
                        

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

                Context.Log(LogLevel.Information, $"Finished handling of {states.Count} Bynder Notifications [{succesful} created/updated | {deleted} deleted | {failed} failed | {retried} retried]");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        public override string Test()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.Test());

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


        #endregion Methods
    }
}