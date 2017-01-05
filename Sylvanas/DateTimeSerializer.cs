using System;
using System.Xml;
using Sylvanas.Converter;
using Sylvanas.Extensions;

namespace Sylvanas
{
    public static class DateTimeSerializer
    {
        public const string ShortDateTimeFormat = "yyyy-MM-dd";
        public const string DateTimeFormatSecondsUtcOffset = "yyyy-MM-ddTHH:mm:sszzz";
        public const string DateTimeFormatSecondsNoOffset = "yyyy-MM-ddTHH:mm:ss";
        public const string XsdDateTimeFormatSeconds = "yyyy-MM-ddTHH:mm:ssZ";
        public const string XsdDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        public const string DateTimeFormatTicksUtcOffset = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
        public const string DateTimeFormatTicksNoUtcOffset = "yyyy-MM-ddTHH:mm:ss.fffffff";
        internal static TimeZoneInfo LocalTimeZone = GetLocalTimeZoneInfo();

        public static TimeZoneInfo GetLocalTimeZoneInfo()
        {
            try
            {
                return TimeZoneInfo.Local;
            }
            catch (Exception)
            {
                return TimeZoneInfo.Utc;
            }
        }

        public static string ToShortestXsdDateTimeString(DateTime dateTime, bool skipDateTimeConversion = false)
        {
            var timeOfDay = dateTime.TimeOfDay;

            var isStartOfDay = timeOfDay.Ticks == 0;
            if (isStartOfDay && !skipDateTimeConversion)
            {
                return dateTime.ToString(ShortDateTimeFormat);
            }

            var hasFractionalSecs = (timeOfDay.Milliseconds != 0) || (timeOfDay.Ticks%TimeSpan.TicksPerMillisecond != 0);

            if (skipDateTimeConversion)
            {
                if (!hasFractionalSecs)
                {
                    return dateTime.Kind == DateTimeKind.Local
                        ? dateTime.ToString(DateTimeFormatSecondsUtcOffset)
                        : dateTime.Kind == DateTimeKind.Unspecified
                            ? dateTime.ToString(DateTimeFormatSecondsNoOffset)
                            : dateTime.ToStableUniversalTime().ToString(XsdDateTimeFormatSeconds);
                }

                return dateTime.Kind == DateTimeKind.Local
                    ? dateTime.ToString(DateTimeFormatTicksUtcOffset)
                    : dateTime.Kind == DateTimeKind.Unspecified
                        ? dateTime.ToString(DateTimeFormatTicksNoUtcOffset)
                        : XmlConvert.ToString(dateTime.ToStableUniversalTime(), XsdDateTimeFormat);
            }

            if (!hasFractionalSecs)
            {
                return dateTime.Kind != DateTimeKind.Utc
                    ? dateTime.ToString(DateTimeFormatSecondsUtcOffset)
                    : dateTime.ToStableUniversalTime().ToString(XsdDateTimeFormatSeconds);
            }

            return dateTime.Kind != DateTimeKind.Utc
                ? dateTime.ToString(DateTimeFormatTicksUtcOffset)
                : XmlConvert.ToString(dateTime.ToStableUniversalTime(), XsdDateTimeFormat);
        }

        public static string ToXsdTimeSpanString(TimeSpan timeSpan)
        {
            return TimeSpanConverter.ToXsdDuration(timeSpan);
        }

        public static string ToXsdTimeSpanString(TimeSpan? timeSpan)
        {
            return timeSpan != null ? ToXsdTimeSpanString(timeSpan.Value) : null;
        }
    }
}