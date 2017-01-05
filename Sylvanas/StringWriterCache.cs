using System;
using System.Globalization;
using System.IO;

namespace Sylvanas
{
    // Use separate cache internally to avoid reallocations and cache misses
    internal static class StringWriterThreadStatic
    {
        [ThreadStatic] private static StringWriter _cache;

        public static StringWriter Allocate()
        {
            var ret = _cache;
            if (ret == null)
            {
                return new StringWriter(CultureInfo.InvariantCulture);
            }

            var sb = ret.GetStringBuilder();
            sb.Length = 0;
            _cache = null;
            return ret;
        }

        public static string ReturnAndFree(StringWriter writer)
        {
            var ret = writer.ToString();
            _cache = writer;
            return ret;
        }
    }
}