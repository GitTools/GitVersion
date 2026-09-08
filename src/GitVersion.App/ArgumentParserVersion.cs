namespace GitVersion;

internal enum ArgumentParserVersion
{
    V6,
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
                $"{RetiredEnvironmentVariableName} has been removed. Unset it and use {EnvironmentVariableName}=v6 for the temporary legacy parser, or {EnvironmentVariableName}=v7 (the default).");
        }

        return FeatureSelector.Resolve(EnvironmentVariableName, "v7", "v6", "v7") == "v6"
            ? ArgumentParserVersion.V6
            : ArgumentParserVersion.V7;
    }
}
