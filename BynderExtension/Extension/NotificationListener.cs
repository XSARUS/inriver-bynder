using Amazon.SimpleNotificationService.Util;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Bynder.Extension
{
    using Bynder.Api;
    using Bynder.Sdk.Model;
    using Bynder.Utils.Helpers;
    using Models;
    using Names;
    using System.Text;

    public class NotificationListener : Extension, IInboundDataExtension
    {
        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = new Dictionary<string, string>();
                
                return settings;
            }
        }

        #region Methods

        /// <summary>
        /// called on POST
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Add(string value) => Update(value);

        /// <summary>
        /// called on DELETE - no implementation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Delete(string value) => string.Empty;

        /// <summary>
        /// called on PUT/POST
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Update(string value)
        {
            // Value is a Amazon SNS message containing the Bynder notification
            // We just store it in a wrapper in the ConnectorState for processing by the ScheduledNotificationHandler and using retry-logic
            AttemptSNSMessageWrapper data = new AttemptSNSMessageWrapper
            {
                Attempt = 1,
                OriginalMessageJson = value // the raw string
            };

            Context.Log(LogLevel.Verbose, $"Original SNS Message in JSON: {value}");
            Context.Log(LogLevel.Verbose, $"Original SNS Message value-test: {data.OriginalMessage.MessageId} -> {data.OriginalMessage.MessageText}");

            ConnectorState state = new ConnectorState
            {
                ConnectorId = ConnectorStateIds.BynderNotificationListener,
                Data = JsonConvert.SerializeObject(data)
            };

            Context.Log(LogLevel.Verbose, $"Wrapped SNS Message in JSON: {state.Data}");

            state = Context.ExtensionManager.UtilityService.AddConnectorState(state);

            string responseMessage = $"Notification message queued in ConnectorState {state.Id} for arbitrary connector {ConnectorStateIds.BynderNotificationListener} at {DateTime.Now.ToString("yyyyMMddHHmmss")}";
            Context.Log(LogLevel.Verbose, responseMessage);

            return responseMessage;
        }

        public override string Test()
        {
            var sb = new StringBuilder();

            if (SettingHelper.ExecuteBaseTestMethod(Context.Settings, Context.Logger))
            {
                sb.AppendLine(base.Test());
            }

            try
            {
                List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(Names.ConnectorStateIds.BynderNotificationListener);
                sb.AppendLine($"Number of connectorstates currently: {states.Count}");
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