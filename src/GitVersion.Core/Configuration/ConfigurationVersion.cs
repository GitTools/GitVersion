namespace GitVersion.Configuration;

internal enum ConfigurationVersion
{
    V6,
    V7
}

internal static class ConfigurationVersionSelector
{
    public const string EnvironmentVariableName = "GITVERSION_CONFIGURATION_VERSION";

    public static ConfigurationVersion Resolve()
    {
        var value = SysEnv.GetEnvironmentVariable(EnvironmentVariableName)?.Trim();

        return value switch
        {
            null or "" => ConfigurationVersion.V7,
            _ when value.Equals("v6", StringComparison.OrdinalIgnoreCase) => ConfigurationVersion.V6,
            _ when value.Equals("v7", StringComparison.OrdinalIgnoreCase) => ConfigurationVersion.V7,
            _ => throw new WarningException(
                $"Unrecognized {EnvironmentVariableName} value '{value}'. Valid values are 'v6' and 'v7'.")
        };
    }

    public static string ResolveName() => Resolve() == ConfigurationVersion.V6 ? "v6" : "v7";
}
