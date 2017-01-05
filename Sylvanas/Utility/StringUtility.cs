namespace Sylvanas.Utility
{
    public static class StringUtility
    {
        public const char MapStartChar = '{';
        public const char MapKeySeperator = ':';
        public const char ItemSeperator = ',';
        public const char MapEndChar = '}';
        public const string MapNullValue = "\"\"";
        public const string EmptyMap = "{}";

        public const char ListStartChar = '[';
        public const char ListEndChar = ']';
        public const char ReturnChar = '\r';
        public const char LineFeedChar = '\n';

        public const char QuoteChar = '"';
        public const string QuoteString = "\"";
        public const string EscapedQuoteString = "\\\"";
        public const string ItemSeperatorString = ",";
        public const string MapKeySeperatorString = ":";
        public const string DoubleQuoteString = "\"\"";

        private const int LengthFromLargestChar = '}' + 1;

        public static readonly char[] EscapeChars =
        {
            QuoteChar, MapKeySeperator, ItemSeperator, MapStartChar, MapEndChar, ListStartChar, ListEndChar, ReturnChar,
            LineFeedChar
        };

        private static readonly bool[] EscapeCharFlags = new bool[LengthFromLargestChar];

        static StringUtility()
        {
            foreach (var escapeChar in EscapeChars)
            {
                EscapeCharFlags[escapeChar] = true;
            }
        }

        public static bool HasAnyEscapeChars(string value)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c >= LengthFromLargestChar || !EscapeCharFlags[c])
                {
                    continue;
                }
                return true;
            }

            return false;
        }
    }
}