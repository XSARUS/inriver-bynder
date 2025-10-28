using Amazon.SimpleNotificationService.Util;
using Newtonsoft.Json;

namespace Bynder.Models
{
    public class AttemptSNSMessageWrapper
    {
        #region Properties

        public int Attempt { get; set; }
        public string OriginalMessageJson { get; set; }

        [JsonIgnore]
        public Message OriginalMessage => Message.ParseMessage(OriginalMessageJson);

        #endregion Properties
    }
}