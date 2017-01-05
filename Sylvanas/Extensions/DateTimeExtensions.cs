using System;

namespace Sylvanas.Extensions
{
    public static class DateTimeExtensions
    {
        public const long UnixEpoch = 621355968000000000L;
        private static readonly DateTime UnixEpochDateTimeUtc = new DateTime(UnixEpoch, DateTimeKind.Utc);
        private static readonly DateTime MinDateTimeUtc = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(this DateTime dateTime)
        {
            return dateTime.ToDateTimeSinceUnixEpoch().Ticks/TimeSpan.TicksPerSecond;
        }

        public static long ToUnixTimeMs(this DateTime dateTime)
        {
            var universal = ToDateTimeSinceUnixEpoch(dateTime);
            return (long) universal.TotalMilliseconds;
        }

        private static TimeSpan ToDateTimeSinceUnixEpoch(this DateTime dateTime)
        {
            var dtUtc = dateTime;
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                dtUtc = dateTime.Kind == DateTimeKind.Unspecified && dateTime > DateTime.MinValue
                    ? DateTime.SpecifyKind(dateTime.Subtract(DateTimeSerializer.LocalTimeZone.GetUtcOffset(dateTime)),
                        DateTimeKind.Utc)
                    : dateTime.ToStableUniversalTime();
            }

            var universal = dtUtc.Subtract(UnixEpochDateTimeUtc);
            return universal;
        }

        public static DateTime ToStableUniversalTime(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }
            if (dateTime == DateTime.MinValue)
            {
                return MinDateTimeUtc;
            }

            return dateTime.ToUniversalTime();
        }
    }
}