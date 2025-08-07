using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;

namespace Bynder.Extension
{
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
            ConnectorState state = new ConnectorState
            {
                ConnectorId = ConnectorStateIds.BynderNotificationListener,
                Data = JsonConvert.SerializeObject(value)
            };

            state = Context.ExtensionManager.UtilityService.AddConnectorState(state);

            string responseMessage = $"Notification message queued in ConnectorState {state.Id} for arbitrary connector {ConnectorStateIds.BynderNotificationListener} at {state.Created}";
            Context.Log(LogLevel.Verbose, responseMessage);

            return responseMessage;
        }

        #endregion Methods
    }
}