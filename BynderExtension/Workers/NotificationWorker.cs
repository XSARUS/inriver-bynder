using Amazon.SimpleNotificationService.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bynder.Workers
{
    using Enums;
    using Models;
    using SettingProviders;

    public class NotificationWorker : IWorker
    {
        #region Fields

        private readonly Dictionary<string, NotificationType> _notificationMapping = new Dictionary<string, NotificationType>
        {
            { "asset_bank.media.updated", NotificationType.DataUpsert },
            { "asset_bank.media.uploaded", NotificationType.DataUpsert },
            { "asset_bank.media.pre_archived", NotificationType.DataUpsert },
            { "asset_bank.media.upload", NotificationType.DataUpsert },
            { "asset_bank.media.create", NotificationType.DataUpsert },
            { "asset_bank.media.meta_updated", NotificationType.MetadataUpdated },
            { "asset_bank.media.deleted", NotificationType.IsDeleted },
            { "asset_bank.media.archived", NotificationType.IsArchived },
        };

        #endregion Fields

        #region Properties

        public Dictionary<string, string> DefaultSettings => NotificationWorkerSettingsProvider.Create();

        #endregion Properties

        #region Methods

        public NotificationWorkerResult Execute(string requestBody)
        {
            var result = new NotificationWorkerResult();
            var snsMessage = Message.ParseMessage(requestBody) ?? throw new ArgumentException("Cannot parse Request Body as AWS SNS message");

            // check if (initial) subscription type
            if (snsMessage.IsSubscriptionType && snsMessage.IsMessageSignatureValid())
            {
                result.Messages.Add($"AWS SNS Subscription message received [{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}]");
                snsMessage.SubscribeToTopic();
                result.Messages.Add($"AWS SNS Subscription acknowleged [{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}]");
                return result;
            }

            // check if notification & notification topic is expected
            if (snsMessage.IsNotificationType)
            {
                if (!_notificationMapping.ContainsKey(snsMessage.Subject))
                {
                    result.Messages.Add($"AWS SNS - Not acting on subject {snsMessage.Subject} [{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}]");
                    return result;
                }

                result.NotificationType = _notificationMapping[snsMessage.Subject];

                dynamic innerMessage = JsonConvert.DeserializeObject(snsMessage.MessageText);
                if (!string.IsNullOrEmpty(innerMessage?.media_id?.ToString()))
                {
                    var mediaId = innerMessage.media_id.ToString();
                    result.Messages.Add($"AWS SNS - Notification type '{result.NotificationType}' for media_id '{mediaId}' [{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}]");
                    result.MediaId = mediaId;
                    return result;
                }
            }

            return result;
        }

        #endregion Methods
    }
}