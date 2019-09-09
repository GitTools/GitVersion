namespace GitVersion.Helpers
{
    public static class ServiceMessageEscapeHelper
    {
        public static string EscapeValue(string value)
        {
            if (value == null)
            {
                return null;
            }
            // List of escape values from http://confluence.jetbrains.com/display/TCD8/Build+Script+Interaction+with+TeamCity

            value = value.Replace("|", "||");
            value = value.Replace("'", "|'");
            value = value.Replace("[", "|[");
            value = value.Replace("]", "|]");
            value = value.Replace("\r", "|r");
            value = value.Replace("\n", "|n");

            return value;
        }
    }
}