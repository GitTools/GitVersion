using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion;

internal sealed record FeatureSelections(ArgumentParserVersion ArgumentParser, ConfigurationVersion Configuration, GitBackend GitBackend)
{
    public static FeatureSelections Resolve() => new(
        ArgumentParserVersionSelector.Resolve(), ConfigurationVersionSelector.Resolve(), GitBackendSelector.Resolve());

    public void Log(ILogger logger) =>
        logger.LogInformation(
            "Argument parser version: {ArgumentParserVersion}; Configuration version: {ConfigurationVersion}; Git backend: {GitBackend}",
            ArgumentParser == ArgumentParserVersion.V6 ? "v6" : "v7",
            Configuration == ConfigurationVersion.V6 ? "v6" : "v7",
            GitBackend == GitBackend.Managed ? "managed" : "libgit2");
}
