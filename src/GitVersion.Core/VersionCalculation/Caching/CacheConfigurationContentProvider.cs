using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Caching;

internal sealed class CacheConfigurationContentProvider(
    IFileSystem fileSystem,
    IOptions<GitVersionOptions> options,
    IConfigurationFileLocator configFileLocator,
    IConfigurationSerializer configurationSerializer,
    IGitRepositoryInfo repositoryInfo)
{
    public string GetFileContent()
    {
        // Hash the contents, so moving an unchanged configuration between these
        // locations does not invalidate the cache.
        var configFilePath = configFileLocator.GetConfigurationFile(options.Value.WorkingDirectory)
                             ?? configFileLocator.GetConfigurationFile(repositoryInfo.ProjectRootDirectory);
        return configFilePath != null && fileSystem.File.Exists(configFilePath)
            ? fileSystem.File.ReadAllText(configFilePath)
            : string.Empty;
    }

    // Serialize independently of the command-line representation.
    public string GetOverrideContent(IReadOnlyDictionary<object, object?>? overrideConfiguration) =>
        overrideConfiguration?.Any() == true
            ? configurationSerializer.Serialize(overrideConfiguration)
            : string.Empty;
}
