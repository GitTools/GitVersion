using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion;

internal sealed record FeatureSelections(ArgumentParserVersion ArgumentParser, ConfigurationVersion Configuration, GitBackend GitBackend)
{
    public static FeatureSelections Resolve() => new(
        ArgumentParserVersionSelector.Resolve(), ConfigurationVersionSelector.Resolve(), ResolveGitBackend());

    private static GitBackend ResolveGitBackend()
    {
#if GITVERSION_MANAGED_ONLY
        var value = SysEnv.GetEnvironmentVariable(GitBackendSelector.EnvironmentVariableName)?.Trim();
        if (!string.IsNullOrEmpty(value) && !value.Equals("managed", StringComparison.OrdinalIgnoreCase))
        {
            throw new WarningException($"This executable supports only the managed Git backend. Unset {GitBackendSelector.EnvironmentVariableName} or set it to 'managed'.");
        }
        return GitBackend.Managed;
#else
        return GitBackendSelector.Resolve();
#endif
    }

    public void Log(ILogger logger) =>
        logger.LogInformation(
            "Argument parser version: {ArgumentParserVersion}; Configuration version: {ConfigurationVersion}; Git backend: {GitBackend}",
            ArgumentParser == ArgumentParserVersion.V6 ? "v6" : "v7",
            Configuration == ConfigurationVersion.V6 ? "v6" : "v7",
            "managed");
}
