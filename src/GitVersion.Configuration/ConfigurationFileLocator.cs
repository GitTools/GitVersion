using GitVersion.Extensions;
using GitVersion.Helpers;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration;

internal class ConfigurationFileLocator(IFileSystem fileSystem, IOptions<GitVersionOptions> options)
    : IConfigurationFileLocator
{
    public const string DefaultFileName = "GitVersion.yml";
    public const string DefaultAlternativeFileName = "GitVersion.yaml";

    private readonly string? configurationFile = options.Value.ConfigurationInfo.ConfigurationFile;

    public bool TryGetConfigurationFile(string? workingDirectory, string? projectRootDirectory, out string? configFilePath)
        =>
            HasConfigurationFile(workingDirectory, out configFilePath)
            || HasConfigurationFile(projectRootDirectory, out configFilePath);

    public void Verify(string? workingDirectory, string? projectRootDirectory)
    {
        if (Path.IsPathRooted(this.configurationFile)) return;
        if (PathHelper.Equal(workingDirectory, projectRootDirectory)) return;
        WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
    }

    public IGitVersionConfiguration ReadConfiguration(string? configFilePath)
    {
        if (configFilePath == null || !fileSystem.Exists(configFilePath)) return GitHubFlowConfigurationBuilder.New.Build();

        var readAllText = fileSystem.ReadAllText(configFilePath);
        return ConfigurationSerializer.Read(new StringReader(readAllText));
    }

    public IReadOnlyDictionary<object, object?>? ReadOverrideConfiguration(string? configFilePath)
    {
        if (configFilePath == null || !fileSystem.Exists(configFilePath)) return null;

        var readAllText = fileSystem.ReadAllText(configFilePath);

        return ConfigurationSerializer.Deserialize<Dictionary<object, object?>>(readAllText);
    }

    private bool HasConfigurationFile(string? workingDirectory, out string? path)
    {
        bool HasConfigurationFileAt(string fileName, out string? configFile)
        {
            configFile = null;
            if (!fileSystem.Exists(PathHelper.Combine(workingDirectory, fileName))) return false;

            configFile = PathHelper.Combine(workingDirectory, fileName);
            return true;
        }

        path = null;
        if (workingDirectory is null) return false;
        return !this.configurationFile.IsNullOrWhiteSpace()
            ? HasConfigurationFileAt(this.configurationFile, out path)
            : HasConfigurationFileAt(DefaultFileName, out path)
              || HasConfigurationFileAt(DefaultAlternativeFileName, out path);
    }

    private void WarnAboutAmbiguousConfigFileSelection(string? workingDirectory, string? projectRootDirectory)
    {
        TryGetConfigurationFile(workingDirectory, null, out var workingConfigFile);
        TryGetConfigurationFile(null, projectRootDirectory, out var projectRootConfigFile);

        var hasConfigInWorkingDirectory = workingConfigFile != null && fileSystem.Exists(workingConfigFile);
        var hasConfigInProjectRootDirectory = projectRootConfigFile != null && fileSystem.Exists(projectRootConfigFile);

        if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
        {
            throw new WarningException($"Ambiguous configuration file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
        }

        if (!hasConfigInProjectRootDirectory && !hasConfigInWorkingDirectory)
        {
            if (this.configurationFile != DefaultFileName && this.configurationFile != DefaultAlternativeFileName)
            {
                workingConfigFile = PathHelper.Combine(workingDirectory, this.configurationFile);
                projectRootConfigFile = PathHelper.Combine(projectRootDirectory, this.configurationFile);
                throw new WarningException($"The configuration file was not found at '{workingConfigFile}' or '{projectRootConfigFile}'");
            }
        }
    }
}
