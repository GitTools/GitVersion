using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;
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
        if (!Path.IsPathRooted(this.configurationFile) && !fileSystem.PathsEqual(workingDirectory, projectRootDirectory))
            WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
    }

    public IGitVersionConfiguration ReadConfiguration(string? configFilePath)
    {
        if (configFilePath == null || !fileSystem.Exists(configFilePath)) return new GitVersionConfiguration();

        var readAllText = fileSystem.ReadAllText(configFilePath);
        var readConfig = ConfigurationSerializer.Read(new StringReader(readAllText));

        VerifyReadConfig(readConfig);

        return readConfig;
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

    private static void VerifyReadConfig(IGitVersionConfiguration configuration)
    {
        // Verify no branches are set to TrunkBased mode
        if (configuration.Branches.Any(b => b.Value.VersioningMode == VersioningMode.TrunkBased))
        {
            throw new ConfigurationException(@"TrunkBased mode only works at the repository level, a single branch cannot be put into TrunkBased mode

This is because TrunkBased mode treats your entire git repository as an event source with each merge into the 'TrunkBased' incrementing the version.

If the docs do not help you decide on the mode open an issue to discuss what you are trying to do.");
        }
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
