namespace GitFlowVersion
{
    static class ExtensionMethods
    {
        public static string TrimNewLines(this string s)
        {
            return s.TrimEnd('\r', '\n');
        }
    }
}