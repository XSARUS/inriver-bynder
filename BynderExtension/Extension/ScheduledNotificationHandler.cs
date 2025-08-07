using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Extension
{
    using Enums;
    using Workers;

    public class ScheduledNotificationHandler : AbstractScheduledExtension
    {
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
                int failed = 0;
                int succesful = 0;

                foreach (ConnectorState state in states.OrderBy(s => s.Created))
                {
                    string result = string.Empty;
                    var notificationMessage = JsonConvert.DeserializeObject<string>(state.Data);

                    try
                    {
                        Context.Log(LogLevel.Debug, $"Handling Bynder Notifications of ConnectorState {state.Id} created at {state.Created}");

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
                    }
                    catch (Exception e)
                    {
                        Context.Log(LogLevel.Error, $"Failed handling Bynder Notifications of ConnectorState {state.Id} created at {state.Created}: {e.Message}", e);
                        Context.Log(LogLevel.Verbose, state.Data);
                        failed++;
                    }
                    finally
                    {
                        Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                    }
                }

                Context.Log(LogLevel.Information, $"Finished handling of {states.Count} Bynder Notifications [{succesful} successfull | {failed} failed]");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods
    }
}