using System;

namespace Bynder.Models
{
    public class TimestampSettings
    {
        #region Properties

        public bool LocalDstEnabled { get; set; } = true;
        public string LocalTimeZone { get; set; } = "W. Europe Standard Time";
        public DateTimeKind TimstampType { get; set; } = DateTimeKind.Utc;

        #endregion Properties
    }
}