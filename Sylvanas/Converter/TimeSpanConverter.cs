using System;

namespace Sylvanas.Converter
{
    public class TimeSpanConverter
    {
        public static string ToXsdDuration(TimeSpan timeSpan)
        {
            var sb = StringBuilderThreadStatic.Allocate();

            sb.Append(timeSpan.Ticks < 0 ? "-P" : "P");

            double ticks = Math.Abs(timeSpan.Ticks);
            var totalSeconds = ticks/TimeSpan.TicksPerSecond;
            var wholeSeconds = (int) totalSeconds;
            var seconds = wholeSeconds;
            var sec = seconds >= 60 ? seconds%60 : seconds;
            var min = (seconds = seconds/60) >= 60 ? seconds%60 : seconds;
            var hours = (seconds = seconds/60) >= 24 ? seconds%24 : seconds;
            var days = seconds/24;
            var remainingSecs = sec + (totalSeconds - wholeSeconds);

            if (days > 0)
            {
                sb.Append(days + "D");
            }

            if (days == 0 || hours + min + sec + remainingSecs > 0)
            {
                sb.Append("T");
                if (hours > 0)
                {
                    sb.Append(hours + "H");
                }
                if (min > 0)
                {
                    sb.Append(min + "M");
                }

                if (remainingSecs > 0)
                {
                    var secFmt = $"{remainingSecs:0.0000000}";
                    secFmt = secFmt.TrimEnd('0').TrimEnd('.');
                    sb.Append(secFmt + "S");
                }
                else if (sb.Length == 2)
                {
                    sb.Append("0S");
                }
            }

            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }
    }
}