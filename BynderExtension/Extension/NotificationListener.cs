using System;
using Bynder.Workers;
using inRiver.Remoting.Extension.Interface;

namespace Bynder.Extension
{
    public class NotificationListener : Extension, IInboundDataExtension
    {
        /// <summary>
        /// called on POST
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Add(string value) => Update(value);

        /// <summary>
        /// called on PUT/POST
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Update(string value)
        {
            // first, handle the notification
            var notificationWorker = Container.GetInstance<NotificationWorker>();
            var notificationResult = notificationWorker.Execute(value);
            var resultMessages = notificationResult.Messages;

            // if the outcome of the notification contains a media Id, we need to start handling the asset
            if (!string.IsNullOrEmpty(notificationResult.MediaId))
            {
                var assetWorker = Container.GetInstance<AssetUpdatedWorker>();
                var updaterResult = assetWorker.Execute(notificationResult.MediaId);
                resultMessages.AddRange(updaterResult.Messages);
            }

            // return the outcome to the caller
            return string.Join(Environment.NewLine, resultMessages);
        }

        /// <summary>
        /// called on DELETE - no implementation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Delete(string value) => string.Empty;

    }
}
