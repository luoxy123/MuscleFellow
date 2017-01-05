using System;
using System.Text;

namespace Sylvanas
{
    // Use separate cache internally to avoid reallocations and cache misses
    internal static class StringBuilderThreadStatic
    {
        [ThreadStatic] private static StringBuilder _cache;

        public static StringBuilder Allocate()
        {
            var ret = _cache;
            if (ret == null)
            {
                return new StringBuilder();
            }

            ret.Length = 0;
            _cache = null; // don't re-issue cached instance until it's freed
            return ret;
        }

        public static string ReturnAndFree(StringBuilder sb)
        {
            var ret = sb.ToString();
            _cache = sb;
            return ret;
        }
    }
}