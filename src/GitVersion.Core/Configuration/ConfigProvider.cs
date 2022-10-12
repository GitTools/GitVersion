using GitVersion.Configuration.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration;

public class ConfigProvider : IConfigProvider
{
    private readonly IFileSystem fileSystem;
    private readonly ILog log;
    private readonly IConfigFileLocator configFileLocator;
    private readonly IOptions<GitVersionOptions> options;
    private readonly IConfigInitWizard configInitWizard;
    private readonly IGitRepositoryInfo repositoryInfo;

    public ConfigProvider(IFileSystem fileSystem, ILog log, IConfigFileLocator configFileLocator,
        IOptions<GitVersionOptions> options, IConfigInitWizard configInitWizard, IGitRepositoryInfo repositoryInfo)
    {
        this.fileSystem = fileSystem.NotNull();
        this.log = log.NotNull();
        this.configFileLocator = configFileLocator.NotNull();
        this.options = options.NotNull();
        this.configInitWizard = configInitWizard.NotNull();
        this.repositoryInfo = repositoryInfo.NotNull();
    }

    public GitVersionConfiguration Provide(GitVersionConfiguration? overrideConfig = null)
    {
        var gitVersionOptions = this.options.Value;
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = this.repositoryInfo.ProjectRootDirectory;

        var rootDirectory = this.configFileLocator.HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory;
        return Provide(rootDirectory, overrideConfig);
    }

    public GitVersionConfiguration Provide(string? workingDirectory, GitVersionConfiguration? overrideConfig = null)
    {
        var configurationBuilder = new ConfigurationBuilder();
        if (workingDirectory != null)
            configurationBuilder = configurationBuilder.Add(this.configFileLocator.ReadConfig(workingDirectory));
        return configurationBuilder
            .Add(overrideConfig ?? new GitVersionConfiguration())
            .Build();
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
        ConfigSerializer.Write(configuration, writer);
        stream.Flush();
    }
}
