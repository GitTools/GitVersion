using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration;

public class ConfigurationFileLocator : IConfigurationFileLocator
{
    public const string DefaultFileName = "GitVersion.yml";
    private readonly IFileSystem fileSystem;
    public ConfigurationFileLocator(IFileSystem fileSystem, IOptions<GitVersionOptions> options)
    {
        this.fileSystem = fileSystem;
        var configFile = options.Value.ConfigInfo.ConfigurationFile;
        FilePath = !configFile.IsNullOrWhiteSpace() ? configFile : DefaultFileName;
    }

    public string FilePath { get; }

    public bool HasConfigFileAt(string workingDirectory) => this.fileSystem.Exists(PathHelper.Combine(workingDirectory, FilePath));

    public string? GetConfigFilePath(string? workingDirectory) => workingDirectory != null ? PathHelper.Combine(workingDirectory, FilePath) : null;

    public void Verify(string? workingDirectory, string? projectRootDirectory)
    {
        if (!Path.IsPathRooted(FilePath) && !this.fileSystem.PathsEqual(workingDirectory, projectRootDirectory))
        {
            WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
        }
    }

    public string? SelectConfigFilePath(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo)
    {
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = repositoryInfo.ProjectRootDirectory;

        return GetConfigFilePath(HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory);
    }

    public GitVersionConfiguration ReadConfig(string workingDirectory)
    {
        var configFilePath = GetConfigFilePath(workingDirectory);

        if (configFilePath != null && this.fileSystem.Exists(configFilePath))
        {
            var readAllText = this.fileSystem.ReadAllText(configFilePath);
            var readConfig = ConfigurationSerializer.Read(new StringReader(readAllText));

            VerifyReadConfig(readConfig);

            return readConfig;
        }

        return new GitVersionConfiguration();
    }

    public IReadOnlyDictionary<object, object?>? ReadOverrideConfiguration(string? workingDirectory)
    {
        var configFilePath = GetConfigFilePath(workingDirectory);

        Dictionary<object, object?>? configuration = null;
        if (configFilePath != null && this.fileSystem.Exists(configFilePath))
        {
            var readAllText = this.fileSystem.ReadAllText(configFilePath);

            configuration = ConfigurationSerializer.Deserialize<Dictionary<object, object?>>(readAllText);
        }

        return configuration;
    }

    public void Verify(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo)
    {
        if (!gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
        {
            // Assuming this is a dynamic repository. At this stage it's unsure whether we have
            // any .git info so we need to skip verification
            return;
        }

        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = repositoryInfo.ProjectRootDirectory;

        Verify(workingDirectory, projectRootDirectory);
    }

    private static void VerifyReadConfig(GitVersionConfiguration configuration)
    {
        // Verify no branches are set to mainline mode
        if (configuration.Branches.Any(b => b.Value.VersioningMode == VersioningMode.Mainline))
        {
            throw new ConfigurationException(@"Mainline mode only works at the repository level, a single branch cannot be put into mainline mode

This is because mainline mode treats your entire git repository as an event source with each merge into the 'mainline' incrementing the version.

If the docs do not help you decide on the mode open an issue to discuss what you are trying to do.");
        }
    }

    private void WarnAboutAmbiguousConfigFileSelection(string? workingDirectory, string? projectRootDirectory)
    {
        var workingConfigFile = GetConfigFilePath(workingDirectory);
        var projectRootConfigFile = GetConfigFilePath(projectRootDirectory);

        var hasConfigInWorkingDirectory = workingConfigFile != null && this.fileSystem.Exists(workingConfigFile);
        var hasConfigInProjectRootDirectory = projectRootConfigFile != null && this.fileSystem.Exists(projectRootConfigFile);

        if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
        {
            throw new WarningException($"Ambiguous configuration file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
        }

        if (!hasConfigInProjectRootDirectory && !hasConfigInWorkingDirectory)
        {
            if (FilePath != DefaultFileName)
                throw new WarningException($"The configuration file was not found at '{workingConfigFile}' or '{projectRootConfigFile}'");
        }
    }
}
