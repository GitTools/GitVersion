namespace GitFlowVersion
{
    static class ExtensionMethods
    {
        public static string TrimNewLines(this string s)
        {
            return s.TrimEnd('\r', '\n');
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
    }
}