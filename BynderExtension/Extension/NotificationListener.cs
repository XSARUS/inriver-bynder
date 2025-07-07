using System.Collections.Generic;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using System;
using System.Threading;

namespace Bynder.Extension
{
    using Config;
    using Enums;
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
            return $"[OK] Notification message queued";

            string result = string.Empty;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start timing
            Context.Log(LogLevel.Verbose, $"NotificationListener START...");

            try
            {
                // wait x seconds because the export db of Bynder does not immediately have the change synced.
                Thread.Sleep(NotificationListenerThreadSleepMilliSeconds);

                // log the incoming notification
                Context.Log(LogLevel.Verbose, $"Notification received: {value}");

                var notificationWorker = Container.GetInstance<NotificationWorker>();

                var stepWatch = System.Diagnostics.Stopwatch.StartNew();
                var notificationResult = notificationWorker.Execute(value);
                stepWatch.Stop();
                Context.Log(LogLevel.Verbose, $"NotificationWorker.Execute took {stepWatch.ElapsedMilliseconds} ms");

                var resultMessages = notificationResult.Messages;

                if (!string.IsNullOrEmpty(notificationResult.MediaId))
                {
                    WorkerResult workerResult;
                    stepWatch.Restart();

                    if (notificationResult.NotificationType == NotificationType.IsDeleted)
                    {
                        var assetDeletedWorker = Container.GetInstance<AssetDeletedWorker>();
                        workerResult = assetDeletedWorker.Execute(notificationResult.MediaId);
                        Context.Log(LogLevel.Verbose, $"AssetDeletedWorker.Execute took {stepWatch.ElapsedMilliseconds} ms");
                    }
                    else
                    {
                        var assetWorker = Container.GetInstance<AssetUpdatedWorker>();
                        workerResult = assetWorker.Execute(notificationResult.MediaId, notificationResult.NotificationType);
                        Context.Log(LogLevel.Verbose, $"AssetUpdatedWorker.Execute took {stepWatch.ElapsedMilliseconds} ms");
                    }

                    resultMessages.AddRange(workerResult.Messages);
                }

                result = string.Join(Environment.NewLine, resultMessages);
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Notification Listener exception occurred: " + ex.GetBaseException().Message, ex);
            }

            stopwatch.Stop(); // Stop overall timer
            Context.Log(LogLevel.Verbose, $"NotificationListener completed in {stopwatch.ElapsedMilliseconds} ms");
            Context.Log(LogLevel.Verbose, $"NotificationListener RESULT: {result}");

            return result;
        }


        #endregion Methods
    }
}