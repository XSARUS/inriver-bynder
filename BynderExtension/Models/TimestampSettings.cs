using System;

namespace Bynder.Models
{
    public class TimestampSettings
    {
        public DateTimeKind TimstampType { get; set; } = DateTimeKind.Utc;
        public string LocalTimeZone { get; set; } = "W. Europe Standard Time";
        public bool LocalDstEnabled { get; set; } = true;
    }
}
