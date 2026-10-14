using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion;

internal sealed class ConfigurationQueryExecutor(
    IConsole console,
    IConfigurationFileLocator configurationFileLocator,
    IConfigurationProvider configurationProvider,
    IConfigurationSerializer configurationSerializer,
    IGitRepositoryInfo repositoryInfo)
{
    private readonly IConsole console = console.NotNull();
    private readonly IConfigurationFileLocator configurationFileLocator = configurationFileLocator.NotNull();
    private readonly IConfigurationProvider configurationProvider = configurationProvider.NotNull();
    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public bool TryExecute(GitVersionOptions gitVersionOptions)
    {
        var configurationInfo = gitVersionOptions.ConfigurationInfo;
        if (!configurationInfo.ShowConfiguration && configurationInfo.PropertyPath is null)
        {
            return false;
        }

        var version = ConfigurationVersionSelector.Resolve();
        if (configurationInfo.PropertyPath is not null && version != ConfigurationVersion.V7)
        {
            throw new ConfigurationException("Configuration property queries require v7 configuration. Set GITVERSION_CONFIGURATION_VERSION=v7.");
        }

        if (gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
        {
            this.configurationFileLocator.Verify(gitVersionOptions.WorkingDirectory, this.repositoryInfo.ProjectRootDirectory);
        }

        var configurationValue = this.configurationProvider.Provide(version == ConfigurationVersion.V7 ? configurationInfo.OverrideConfiguration : null);
        var serializedConfiguration = configurationInfo.PropertyPath is { } path
            ? this.configurationSerializer.SerializeProperty(configurationValue, path)
            : this.configurationSerializer.Serialize(configurationValue);
        this.console.WriteLine(serializedConfiguration);
        return true;
    }
}
