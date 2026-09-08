namespace GitVersion.Configuration;

internal enum ConfigurationVersion
{
    V6,
    V7
}

internal static class ConfigurationVersionSelector
{
    public const string EnvironmentVariableName = "GITVERSION_CONFIGURATION_VERSION";

    public static ConfigurationVersion Resolve() =>
        FeatureSelector.Resolve(EnvironmentVariableName, "v7", "v6", "v7") == "v6"
            ? ConfigurationVersion.V6
            : ConfigurationVersion.V7;

    public static string ResolveName() => Resolve() == ConfigurationVersion.V6 ? "v6" : "v7";

    public static bool IsExplicitV6() =>
        SysEnv.GetEnvironmentVariable(EnvironmentVariableName)?.Trim().Equals("v6", StringComparison.OrdinalIgnoreCase) ?? false;
}
