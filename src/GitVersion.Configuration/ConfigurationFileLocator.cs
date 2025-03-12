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
        if (PathHelper.IsPathRooted(this.ConfigurationFile)) return;
        if (PathHelper.Equal(workingDirectory, projectRootDirectory)) return;
        WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
    }

    public string? GetConfigurationFile(string? directory)
    {
        if (directory is null) return null;

        string[] candidates = !string.IsNullOrWhiteSpace(this.ConfigurationFile)
            ? [this.ConfigurationFile, .. this.SupportedConfigFileNames]
            : this.SupportedConfigFileNames;

        foreach (var fileName in candidates)
        {
            this.log.Debug($"Trying to find configuration file {fileName} at '{directory}'");
            if (directory != null && fileSystem.Directory.Exists(directory))
            {
                var files = fileSystem.Directory.GetFiles(directory);

                var matchingFile = files.FirstOrDefault(file =>
                    string.Equals(fileSystem.Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase));

                if (matchingFile != null)
                {
                    this.log.Info($"Found configuration file at '{matchingFile}'");
                    return matchingFile;
                }
            }

            this.log.Debug($"Configuration file {fileName} not found at '{directory}'");
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

        workingConfigFile = PathHelper.Combine(workingDirectory, this.ConfigurationFile);
        projectRootConfigFile = PathHelper.Combine(projectRootDirectory, this.ConfigurationFile);
        throw new WarningException($"The configuration file was not found at '{workingConfigFile}' or '{projectRootConfigFile}'");
    }
}
