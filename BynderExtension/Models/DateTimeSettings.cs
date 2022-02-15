using System;

namespace Bynder.Models
{
    public class DateTimeSettings
    {
        #region Properties

        public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Utc;
        public bool LocalDstEnabled { get; set; } = true;
        public string LocalTimeZone { get; set; } = "W. Europe Standard Time";

        #endregion Properties
    }
}