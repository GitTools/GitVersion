using GitVersion.Configuration.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration;

public class ConfigurationProvider : IConfigurationProvider
{
    private readonly IFileSystem fileSystem;
    private readonly ILog log;
    private readonly IConfigurationFileLocator configFileLocator;
    private readonly IOptions<GitVersionOptions> options;
    private readonly IConfigInitWizard configInitWizard;

    public ConfigurationProvider(IFileSystem fileSystem, ILog log, IConfigurationFileLocator configFileLocator,
                                 IOptions<GitVersionOptions> options, IConfigInitWizard configInitWizard)
    {
        this.fileSystem = fileSystem.NotNull();
        this.log = log.NotNull();
        this.configFileLocator = configFileLocator.NotNull();
        this.options = options.NotNull();
        this.configInitWizard = configInitWizard.NotNull();
    }

    public GitVersionConfiguration Provide(GitVersionConfiguration? overrideConfiguration)
    {
        var gitVersionOptions = this.options.Value;
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = FindGitDir(workingDirectory)?.WorkingTreeDirectory;

        var rootDirectory = this.configFileLocator.HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory;
        return ProvideInternal(rootDirectory, overrideConfiguration);
    }

    public void Init(string workingDirectory)
    {
        var configFilePath = this.configFileLocator.GetConfigFilePath(workingDirectory);
        var currentConfiguration = this.configFileLocator.ReadConfig(workingDirectory);

        var configuration = this.configInitWizard.Run(currentConfiguration, workingDirectory);
        if (configuration == null || configFilePath == null) return;

        using var stream = this.fileSystem.OpenWrite(configFilePath);
        using var writer = new StreamWriter(stream);
        this.log.Info("Saving configuration file");
        ConfigurationSerializer.Write(configuration, writer);
        stream.Flush();
    }

    internal GitVersionConfiguration ProvideInternal(string? workingDirectory, GitVersionConfiguration? overrideConfiguration = null)
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (workingDirectory != null)
            configurationBuilder = configurationBuilder.Add(this.configFileLocator.ReadConfig(workingDirectory));

        if (overrideConfiguration != null)
            configurationBuilder.Add(overrideConfiguration);

        return configurationBuilder.Build();
    }

    private static string? ReadGitDirFromFile(string fileName)
    {
        const string expectedPrefix = "gitdir: ";
        var firstLineOfFile = File.ReadLines(fileName).FirstOrDefault();
        if (firstLineOfFile?.StartsWith(expectedPrefix) ?? false)
        {
            return firstLineOfFile[expectedPrefix.Length..]; // strip off the prefix, leaving just the path
        }

        return null;
    }

    private static (string GitDirectory, string WorkingTreeDirectory)? FindGitDir(string path)
    {
        string? startingDir = path;
        while (startingDir is not null)
        {
            var dirOrFilePath = Path.Combine(startingDir, ".git");
            if (Directory.Exists(dirOrFilePath))
            {
                return (dirOrFilePath, Path.GetDirectoryName(dirOrFilePath)!);
            }
            if (File.Exists(dirOrFilePath))
            {
                string? relativeGitDirPath = ReadGitDirFromFile(dirOrFilePath);
                if (!string.IsNullOrWhiteSpace(relativeGitDirPath))
                {
                    var fullGitDirPath = Path.GetFullPath(Path.Combine(startingDir, relativeGitDirPath));
                    if (Directory.Exists(fullGitDirPath))
                    {
                        return (fullGitDirPath, Path.GetDirectoryName(dirOrFilePath)!);
                    }
                }
            }

            startingDir = Path.GetDirectoryName(startingDir);
        }

        return null;
    }
}
