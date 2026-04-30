using Amazon.SimpleNotificationService.Util;
using Newtonsoft.Json;

namespace Bynder.Models
{
    public class AttemptSNSMessageWrapper
    {
        #region Properties

        private readonly object _lock = new object();
        private Message _originalMessage;

        public int Attempt { get; set; }

        [JsonIgnore]
        public Message OriginalMessage
        {
            get
            {
                if (_originalMessage == null)
                {
                    lock (_lock)
                    {
                        if (_originalMessage == null && !string.IsNullOrEmpty(OriginalMessageJson))
                        {
                            _originalMessage = Message.ParseMessage(OriginalMessageJson);
                        }
                    }
                }

                return _originalMessage;
            }
        }

        public string OriginalMessageJson { get; set; }

        #endregion Properties
    }
}