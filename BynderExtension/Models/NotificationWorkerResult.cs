using Bynder.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
