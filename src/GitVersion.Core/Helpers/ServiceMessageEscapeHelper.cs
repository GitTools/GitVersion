namespace GitVersion.Helpers;

/// <summary>Escapes special characters in values written to TeamCity service messages.</summary>
public static class ServiceMessageEscapeHelper
{
    /// <summary>Returns <paramref name="value"/> with TeamCity service-message special characters escaped, or <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>.</summary>
    public static string? EscapeValue(string? value)
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
