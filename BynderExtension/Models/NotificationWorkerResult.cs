using Bynder.Enums;

namespace Bynder.Models
{
    public class NotificationWorkerResult : WorkerResult
    {
        #region Properties

        public string MediaId { get; set; }
        public NotificationType NotificationType { get; set; }

        #endregion Properties
    }
}