using System;

namespace Bynder.Utils.Helpers
{
    public static class DateTimeHelper
    {

        #region Methods

        /// <summary>
        /// Returns UTC converted to TimeZone
        /// </summary>
        /// <param name="utc"></param>
        /// <param name="timeZone"></param>
        /// <returns></returns>
        private static DateTime GetDateTimeForTimeZone(DateTime utc, TimeZoneInfo timeZone)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        }

        /// <summary>
        /// If timezone is filled then converts it. Otherwise it returns DateTime.Now for the timezone of the server.
        /// Converts the start time from UTC to given TimeZone
        /// Uses Daylight Saving Time if required
        /// </summary>
        /// <param name="timeZoneId"></param>
        /// <param name="dstEnabled"></param>
        /// <returns></returns>
        private static DateTime GetLocalTimeStampForTimezone(string timeZoneId, bool dstEnabled)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId)) return DateTime.Now;

            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return GetLocalTimeStampForTimezone(zone, dstEnabled);
        }

        /// <summary>
        /// If timezone is filled then converts it. Otherwise it returns DateTime.Now for the timezone of the server.
        /// Converts the start time from UTC to given TimeZone
        /// Uses Daylight Saving Time if required
        /// </summary>
        /// <param name="timeZone"></param>
        /// <param name="dstEnabled"></param>
        /// <returns></returns>
        private static DateTime GetLocalTimeStampForTimezone(TimeZoneInfo timeZone, bool dstEnabled)
        {
            if (timeZone == null) return DateTime.Now;

            // conversion of utc to timezone
            var utcDt = DateTime.UtcNow;
            return dstEnabled ? GetDateTimeForTimeZone(utcDt, timeZone) :
                                         GetNonDstDateTimeForTimeZone(utcDt, timeZone);
        }

        /// <summary>
        /// Returns UTC converted to Timezone, without DST (to BaseUtcOffset).
        /// </summary>
        /// <param name="utc"></param>
        /// <param name="timeZone"></param>
        /// <returns></returns>
        private static DateTime GetNonDstDateTimeForTimeZone(DateTime utc, TimeZoneInfo timeZone)
        {
            var utcOffset = new DateTimeOffset(utc, TimeSpan.Zero);
            var timeZoneOffset = utcOffset.ToOffset(timeZone.BaseUtcOffset);
            return timeZoneOffset.DateTime;
        }

        /// <summary>
        /// Returns UTC converted to TimeZone
        /// </summary>
        /// <param name="utc"></param>
        /// <param name="timeZone"></param>
        /// <param name="dstEnabled"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeForTimeZone(DateTime utc, TimeZoneInfo timeZone, bool dstEnabled)
        {
            return dstEnabled ? GetDateTimeForTimeZone(utc, timeZone) :
                                         GetNonDstDateTimeForTimeZone(utc, timeZone);
        }

        /// <summary>
        /// When you want to re-use your zone's, use the one with TimeZoneInfo as param
        /// </summary>
        /// <param name="utc"></param>
        /// <param name="timeZoneId"></param>
        /// <param name="dstEnabled"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeForTimeZone(DateTime utc, string timeZoneId, bool dstEnabled)
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return GetDateTimeForTimeZone(utc, timeZone, dstEnabled);
        }
        /// <summary>
        /// Gets timestamp / datetime of now
        /// utc or local
        /// local is converted to given timezone
        /// </summary>
        /// <param name="dateTimeKind">only local and utc are supported!</param>
        /// <param name="timeZoneId">only used for local conversion</param>
        /// <param name="dstEnabled">only used for local conversion</param>
        /// <returns></returns>
        public static DateTime GetTimestamp(DateTimeKind dateTimeKind, string timeZoneId = "W. Europe Standard Time", bool dstEnabled = true)
        {
            switch (dateTimeKind)
            {
                case DateTimeKind.Local:
                    return GetLocalTimeStampForTimezone(timeZoneId, dstEnabled);
                case DateTimeKind.Utc:
                    return DateTime.UtcNow;
                default:
                    throw new NotSupportedException($"DateTimeKind '{dateTimeKind}' is not supported.");
            }
        }

        /// <summary>
        /// Gets timestamp / datetime of now
        /// utc or local
        /// local is converted to given timezone
        /// </summary>
        /// <param name="dateTimeKind">only local and utc are supported!</param>
        /// <param name="timeZone">only used for local conversion</param>
        /// <param name="dstEnabled">only used for local conversion</param>
        /// <returns></returns>
        public static DateTime GetTimestamp(DateTimeKind dateTimeKind, TimeZoneInfo timeZone, bool dstEnabled = true)
        {
            switch (dateTimeKind)
            {
                case DateTimeKind.Local:
                    return GetLocalTimeStampForTimezone(timeZone, dstEnabled);
                case DateTimeKind.Utc:
                    return DateTime.UtcNow;
                default:
                    throw new NotSupportedException($"DateTimeKind '{dateTimeKind}' is not supported.");
            }
        }

        #endregion Methods

    }
}
