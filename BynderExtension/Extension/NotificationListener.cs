using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using System;
using System.Threading;

namespace Bynder.Extension
{
    using Bynder.Config;
    using Bynder.Enums;
    using System.Collections.Generic;
    using Workers;

    public class NotificationListener : Extension, IInboundDataExtension
    {
        const int defaultThreadSleepValue = 15000;

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = base.DefaultSettings;
                settings[Settings.NotificationListenerThreadSleepMilliSeconds] = defaultThreadSleepValue.ToString();
                
                return settings;
            }
        }

        protected int NotificationListenerThreadSleepMilliSeconds
        {
            get
            {
                if (DefaultSettings.TryGetValue(Settings.NotificationListenerThreadSleepMilliSeconds, out string value) &&
                    int.TryParse(value, out int parsed))
                {
                    return parsed;
                }

                return defaultThreadSleepValue;
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
            // wait x seconds because the export db of Bynder does not immediately has the change synced.
            Thread.Sleep(NotificationListenerThreadSleepMilliSeconds);

            string result = string.Empty;

            try
            {
                // log the incomining notification
                Context.Log(LogLevel.Verbose, $"Notification: {value}");

                // first, handle the notification
                var notificationWorker = Container.GetInstance<NotificationWorker>();
                var notificationResult = notificationWorker.Execute(value);
                var resultMessages = notificationResult.Messages;

                // if the outcome of the notification contains a media Id, we need to start handling the asset
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

                // return the outcome to the caller
                result = string.Join(Environment.NewLine, resultMessages);
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }

            Context.Log(LogLevel.Verbose, result);

            return result;
        }

        #endregion Methods
    }
}