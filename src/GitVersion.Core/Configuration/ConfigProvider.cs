using GitVersion.Configurations.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configurations;
using Microsoft.Extensions.Options;

namespace GitVersion.Configurations;

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

    public Configuration Provide(Configuration? overrideConfig = null)
    {
        var gitVersionOptions = this.options.Value;
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = this.repositoryInfo.ProjectRootDirectory;

        var rootDirectory = this.configFileLocator.HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory;
        return Provide(rootDirectory, overrideConfig);
    }

    public Configuration Provide(string? workingDirectory, Configuration? overrideConfig = null)
    {
        var configurationBuilder = new ConfigurationBuilder();
        if (workingDirectory != null)
            configurationBuilder = configurationBuilder.Add(this.configFileLocator.ReadConfig(workingDirectory));
        return configurationBuilder
            .Add(overrideConfig ?? new Model.Configurations.Configuration())
            .Build();
    }

    public void Init(string workingDirectory)
    {
        var configFilePath = this.configFileLocator.GetConfigFilePath(workingDirectory);
        var currentConfiguration = this.configFileLocator.ReadConfig(workingDirectory);

        var config = this.configInitWizard.Run(currentConfiguration, workingDirectory);
        if (config == null || configFilePath == null) return;

        using var stream = this.fileSystem.OpenWrite(configFilePath);
        using var writer = new StreamWriter(stream);
        this.log.Info("Saving config file");
        ConfigSerializer.Write(config, writer);
        stream.Flush();
    }
}
