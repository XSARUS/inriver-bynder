using Amazon.SimpleNotificationService.Util;

namespace Bynder.Models
{
    public class AttemptSNSMessageWrapper
    {
        #region Properties

        public int Attempt { get; set; }
        public Message OriginalMessage { get; set; }

        #endregion Properties
    }
}