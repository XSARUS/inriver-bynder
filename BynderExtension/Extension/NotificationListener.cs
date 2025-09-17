using Amazon.SimpleNotificationService.Util;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;

namespace Bynder.Extension
{
    using Models;
    using Names;

    public class NotificationListener : Extension, IInboundDataExtension
    {
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
                OriginalMessage = JsonConvert.DeserializeObject<Message>(value),
                Attempt = 1
            };

            ConnectorState state = new ConnectorState
            {
                ConnectorId = ConnectorStateIds.BynderNotificationListener,
                Data = JsonConvert.SerializeObject(data)
            };

            state = Context.ExtensionManager.UtilityService.AddConnectorState(state);

            string responseMessage = $"Notification message queued in ConnectorState {state.Id} for arbitrary connector {ConnectorStateIds.BynderNotificationListener} at {state.Created}";
            Context.Log(LogLevel.Verbose, responseMessage);

            return responseMessage;
        }

        #endregion Methods
    }
}