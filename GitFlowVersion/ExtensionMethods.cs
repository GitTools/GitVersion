namespace GitFlowVersion
{
    using System.Text;
    using JetBrains.Annotations;

    static class ExtensionMethods
    {
        public static string TrimNewLines(this string s)
        {
            return s.TrimEnd('\r', '\n');
        }
        [StringFormatMethod("format")]
        public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
        {
            stringBuilder.AppendFormat(format, args);
            stringBuilder.AppendLine();
        }
        public static string TrimStart(this string value, string toTrim)
        {
            if (!value.StartsWith(toTrim))
            {
                return value;
            }
            var startIndex = toTrim.Length;
            return value.Substring(startIndex);
        }

        public static bool IsOdd(this int number)
        {
            return number % 2 != 0;
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
    }
}