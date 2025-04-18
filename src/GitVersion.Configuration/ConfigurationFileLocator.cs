using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration;

internal class ConfigurationFileLocator(
    IFileSystem fileSystem,
    ILog log,
    IOptions<GitVersionOptions> options)
    : IConfigurationFileLocator
{
    public const string DefaultFileName = "GitVersion.yml";
    public const string DefaultAlternativeFileName = "GitVersion.yaml";
    public const string DefaultFileNameDotted = $".{DefaultFileName}";
    public const string DefaultAlternativeFileNameDotted = $".{DefaultAlternativeFileName}";

    private readonly string[] SupportedConfigFileNames =
    [
        DefaultFileName,
        DefaultAlternativeFileName,
        DefaultFileNameDotted,
        DefaultAlternativeFileNameDotted
    ];

    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly ILog log = log.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    private string? ConfigurationFile => options.Value.ConfigurationInfo.ConfigurationFile;

    public void Verify(string? workingDirectory, string? projectRootDirectory)
    {
        if (FileSystemHelper.Path.IsPathRooted(this.ConfigurationFile)) return;
        if (FileSystemHelper.Path.Equal(workingDirectory, projectRootDirectory)) return;
        WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
    }

    public string? GetConfigurationFile(string? directoryPath)
    {
        var customConfigurationFile = GetCustomConfigurationFilePathIfEligable(directoryPath);
        if (!string.IsNullOrWhiteSpace(customConfigurationFile))
        {
            this.log.Info($"Found configuration file at '{customConfigurationFile}'");
            return customConfigurationFile;
        }

        if (string.IsNullOrWhiteSpace(directoryPath) || !fileSystem.Directory.Exists(directoryPath))
        {
            return null;
        }

        var files = fileSystem.Directory.GetFiles(directoryPath);
        foreach (var fileName in this.SupportedConfigFileNames)
        {
            this.log.Debug($"Trying to find configuration file {fileName} at '{directoryPath}'");
            string? matchingFile = files.FirstOrDefault(file => string.Equals(FileSystemHelper.Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase));
            if (matchingFile != null)
            {
                this.log.Info($"Found configuration file at '{matchingFile}'");
                return matchingFile;
            }
        }

        return null;
    }

    private string? GetCustomConfigurationFilePathIfEligable(string? directoryPath)
    {
        if (!string.IsNullOrWhiteSpace(this.ConfigurationFile))
        {
            var configurationFilePath = this.ConfigurationFile;
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                configurationFilePath = FileSystemHelper.Path.Combine(directoryPath, this.ConfigurationFile);
            }

            if (fileSystem.File.Exists(configurationFilePath))
            {
                return configurationFilePath;
            }
        }

        return null;
    }

    private void WarnAboutAmbiguousConfigFileSelection(string? workingDirectory, string? projectRootDirectory)
    {
        var workingConfigFile = GetConfigurationFile(workingDirectory);
        var projectRootConfigFile = GetConfigurationFile(projectRootDirectory);

        var hasConfigInWorkingDirectory = workingConfigFile is not null;
        var hasConfigInProjectRootDirectory = projectRootConfigFile is not null;

        if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
        {
            throw new WarningException($"Ambiguous configuration file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
        }

        if (hasConfigInProjectRootDirectory || hasConfigInWorkingDirectory || this.SupportedConfigFileNames.Any(entry => entry.Equals(this.ConfigurationFile, StringComparison.OrdinalIgnoreCase))) return;

        workingConfigFile = FileSystemHelper.Path.Combine(workingDirectory, this.ConfigurationFile);
        projectRootConfigFile = FileSystemHelper.Path.Combine(projectRootDirectory, this.ConfigurationFile);
        throw new WarningException($"The configuration file was not found at '{workingConfigFile}' or '{projectRootConfigFile}'");
    }
}
