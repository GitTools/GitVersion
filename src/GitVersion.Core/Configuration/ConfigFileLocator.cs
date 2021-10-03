using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration;

public class ConfigFileLocator : IConfigFileLocator
{
    public const string DefaultFileName = "GitVersion.yml";
    private readonly IFileSystem fileSystem;
    public ConfigFileLocator(IFileSystem fileSystem, IOptions<GitVersionOptions> options)
    {
        this.fileSystem = fileSystem;
        var configFile = options?.Value.ConfigInfo.ConfigFile;
        FilePath = !configFile.IsNullOrWhiteSpace() ? configFile : DefaultFileName;
    }

    public string FilePath { get; }

    public bool HasConfigFileAt(string workingDirectory) => this.fileSystem.Exists(Path.Combine(workingDirectory, FilePath));

    public string GetConfigFilePath(string workingDirectory) => Path.Combine(workingDirectory, FilePath);

    public void Verify(string? workingDirectory, string? projectRootDirectory)
    {
        if (!Path.IsPathRooted(FilePath) && !this.fileSystem.PathsEqual(workingDirectory, projectRootDirectory))
        {
            WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
        }
    }

    public string SelectConfigFilePath(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo)
    {
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = repositoryInfo.ProjectRootDirectory;

        return GetConfigFilePath(HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory);
    }

    public Config ReadConfig(string workingDirectory)
    {
        var configFilePath = GetConfigFilePath(workingDirectory);

        if (this.fileSystem.Exists(configFilePath))
        {
            var readAllText = this.fileSystem.ReadAllText(configFilePath);
            var readConfig = ConfigSerializer.Read(new StringReader(readAllText));

            VerifyReadConfig(readConfig);

            return readConfig;
        }

        return new Config();
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

    private static void VerifyReadConfig(Config config)
    {
        // Verify no branches are set to mainline mode
        if (config.Branches.Any(b => b.Value?.VersioningMode == VersioningMode.Mainline))
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

        var hasConfigInWorkingDirectory = this.fileSystem.Exists(workingConfigFile);
        var hasConfigInProjectRootDirectory = this.fileSystem.Exists(projectRootConfigFile);
        if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
        {
            throw new WarningException($"Ambiguous config file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
        }

        if (!hasConfigInProjectRootDirectory && !hasConfigInWorkingDirectory)
        {
            if (FilePath != DefaultFileName)
                throw new WarningException($"The configuration file was not found at '{workingConfigFile}' or '{projectRootConfigFile}'");
        }
    }
}
