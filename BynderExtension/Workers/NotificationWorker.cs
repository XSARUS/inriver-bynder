using Amazon.SimpleNotificationService.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Bynder.Workers
{
    using Enums;

    public class NotificationWorker : IWorker
    {
        #region Fields

        private readonly Dictionary<string, NotificationType> _notificationMapping = new Dictionary<string, NotificationType>
        {
            { "asset_bank.media.updated", NotificationType.DataUpsert },
            { "asset_bank.media.uploaded", NotificationType.DataUpsert },
            { "asset_bank.media.pre_archived", NotificationType.DataUpsert }, //todo is this correct?
            { "asset_bank.media.upload", NotificationType.DataUpsert },
            { "asset_bank.media.create", NotificationType.DataUpsert },
            { "asset_bank.media.meta_updated", NotificationType.MetadataUpdated },
            { "asset_bank.media.deleted", NotificationType.IsDeleted },
            { "asset_bank.media.archived", NotificationType.IsArchived },
        };

        #endregion Fields

        #region Methods

        public Result Execute(string requestBody)
        {
            var result = new Result();

            var snsMessage = Message.ParseMessage(requestBody);
            if (snsMessage == null) throw new ArgumentException("Cannot parse Request Body as AWS SNS message");

            // check if (initial) subscription type
            if (snsMessage.IsSubscriptionType && snsMessage.IsMessageSignatureValid())
            {
                result.Messages.Add("AWS SNS Subscription message received");
                snsMessage.SubscribeToTopic();
                result.Messages.Add("AWS SNS Subscription acknowleged");
                return result;
            }

            // check if notification & notification topic is expected
            if (snsMessage.IsNotificationType)
            {
                if (!_notificationMapping.ContainsKey(snsMessage.Subject))
                {
                    result.Messages.Add($"AWS SNS - Not acting on subject {snsMessage.Subject}");
                    return result;
                }

                result.NotificationType = _notificationMapping[snsMessage.Subject];

                dynamic innerMessage = JsonConvert.DeserializeObject(snsMessage.MessageText);
                if (!string.IsNullOrEmpty(innerMessage?.media_id?.ToString()))
                {
                    var mediaId = innerMessage.media_id.ToString();
                    result.Messages.Add($"AWS SNS - Media update for media_id '{mediaId}'");
                    result.MediaId = mediaId;
                    return result;
                }
            }

            return result;
        }

        #endregion Methods

        #region Classes

        public class Result : WorkerResult
        {
            #region Properties

            public string MediaId { get; set; }
            public NotificationType NotificationType { get; set; }

            #endregion Properties
        }

        #endregion Classes
    }
}