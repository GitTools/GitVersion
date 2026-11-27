namespace GitVersion;

internal enum ArgumentParserVersion
{
    V7
}

internal static class ArgumentParserVersionSelector
{
    public const string EnvironmentVariableName = "GITVERSION_ARGUMENT_PARSER_VERSION";
    public const string RetiredEnvironmentVariableName = "GITVERSION_USE_V6_ARGUMENT_PARSER";

    public static ArgumentParserVersion Resolve()
    {
        if (SysEnv.GetEnvironmentVariable(RetiredEnvironmentVariableName) is not null)
        {
            throw new WarningException(
                $"{RetiredEnvironmentVariableName} has been removed. Unset it and use POSIX-style arguments with {EnvironmentVariableName}=v7 (the default).");
        }

        var value = SysEnv.GetEnvironmentVariable(EnvironmentVariableName)?.Trim().ToLowerInvariant();
        if (value == "v6")
        {
            throw new WarningException($"{EnvironmentVariableName}=v6 was removed in GitVersion 7.1. Use POSIX-style arguments and unset this variable or set it to v7.");
        }

        _ = FeatureSelector.Resolve(EnvironmentVariableName, "v7", "v7");
        return ArgumentParserVersion.V7;
    }
}
