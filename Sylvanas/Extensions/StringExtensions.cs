using System;
using System.Collections.Generic;
using System.Text;

namespace Sylvanas.Extensions
{
    public static class StringExtensions
    {
        public static string WithTrailingSlash(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path[path.Length - 1] != '/')
            {
                return path + "/";
            }

            return path;
        }

        public static string CombineWith(this string path, params string[] thesePaths)
        {
            if (path == null)
            {
                path = "";
            }
            if (thesePaths.Length == 1 && thesePaths[0] == null)
            {
                return path;
            }

            var startPath = path.Length > 1 ? path.TrimEnd('/', '\\') : path;

            var sb = StringBuilderThreadStatic.Allocate();
            sb.Append(startPath);
            AppendPaths(sb, thesePaths);
            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        private static void AppendPaths(StringBuilder sb, string[] paths)
        {
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (sb.Length > 0 && sb[sb.Length - 1] != '/')
                {
                    sb.Append("/");
                }

                sb.Append(path.Replace('\\', '/').TrimStart('/'));
            }
        }

        public static string UrlEncode(this string text, bool upperCase = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var sb = StringBuilderThreadStatic.Allocate();
            var fmt = upperCase ? "X2" : "x2";

            foreach (var charCode in Encoding.UTF8.GetBytes(text))
            {
                if (
                    charCode >= 65 && charCode <= 90 // A-Z
                    || charCode >= 97 && charCode <= 122 // a-z
                    || charCode >= 48 && charCode <= 57 // 0-9
                    || charCode >= 44 && charCode <= 46 // ,-.
                    )
                {
                    sb.Append((char) charCode);
                }
                else if (charCode == 32)
                {
                    sb.Append('+');
                }
                else
                {
                    sb.Append('%' + charCode.ToString(fmt));
                }
            }

            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static string UrlDecode(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var bytes = new List<byte>();

            var textLength = text.Length;
            for (int i = 0; i < textLength; i++)
            {
                var c = text[i];
                if (c == '+')
                {
                    bytes.Add(32);
                }
                else if (c == '%')
                {
                    var hexNo = Convert.ToByte(text.Substring(i + 1, 2), 16);
                    bytes.Add(hexNo);
                    i += 2;
                }
                else
                {
                    bytes.Add((byte) c);
                }
            }

            byte[] byteArray = bytes.ToArray();
            return Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
        }

        public static byte[] ToAsciiBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
}