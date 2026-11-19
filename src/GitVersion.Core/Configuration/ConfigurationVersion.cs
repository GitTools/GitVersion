namespace GitVersion.Configuration;

internal enum ConfigurationVersion
{
    V7
}

internal static class ConfigurationVersionSelector
{
    public const string EnvironmentVariableName = "GITVERSION_CONFIGURATION_VERSION";

    public static ConfigurationVersion Resolve()
    {
        var value = SysEnv.GetEnvironmentVariable(EnvironmentVariableName)?.Trim().ToLowerInvariant();
        if (value == "v6")
        {
            throw new WarningException($"{EnvironmentVariableName}=v6 was removed in GitVersion 7.1. Unset this variable or set it to v7, then run 'gitversion config migrate' to convert a v6 configuration.");
        }

        _ = FeatureSelector.Resolve(EnvironmentVariableName, "v7", "v7");
        return ConfigurationVersion.V7;
    }

    public static string ResolveName()
    {
        _ = Resolve();
        return "v7";
    }
}
