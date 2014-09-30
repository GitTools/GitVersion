namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using JetBrains.Annotations;

    static class ExtensionMethods
    {
        public static bool IsOdd(this int number)
        {
            return number % 2 != 0;
        }

        public static string TrimToFirstLine(this string s)
        {
            var firstLine = s.Split(new[]
            {
                "\r\n",
                "\n"
            }, StringSplitOptions.None)[0];
            return firstLine.Trim();
        }


        [StringFormatMethod("format")]
        public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
        {
            stringBuilder.AppendFormat(format, args);
            stringBuilder.AppendLine();
        }

        public static string TrimStart(this string value, string toTrim)
        {
            if (!value.StartsWith(toTrim, StringComparison.InvariantCultureIgnoreCase))
            {
                return value;
            }
            var startIndex = toTrim.Length;
            return value.Substring(startIndex);
        }

        public static string JsonEncode(this string value)
        {
            if (value != null)
            {
                return value
                    .Replace("\"", "\\\"")
                    .Replace("\\", "\\\\")
                    .Replace("\b", "\\b")
                    .Replace("\f", "\\f")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t")
                    .Replace("\r", "\\r");
            }
            return null;
        }

        public static string RegexReplace(this string input, string pattern, string replace, RegexOptions options = RegexOptions.None)
        {
            return Regex.Replace(input, pattern, replace, options);
        }

        public static T OnlyOrDefault<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            var list = source as IList<T>;

            if (list != null && list.Count == 1)
            {
                return list[0];
            }

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return default(T);
                var result = e.Current;
                if (!e.MoveNext()) return result;
            }

            return default(T);
        }
    }
}