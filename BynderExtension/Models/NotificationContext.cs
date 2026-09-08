using inRiver.Remoting.Objects;

namespace Bynder.Models
{
    public class NotificationContext
    {
        public ConnectorState State { get; set; }

        public AttemptSNSMessageWrapper StateData { get; set; }

        public NotificationWorkerResult Result { get; set; }
    }
}
