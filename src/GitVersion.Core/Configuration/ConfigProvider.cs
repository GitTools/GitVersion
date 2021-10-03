using GitVersion.Configuration.Init.Wizard;
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
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.configFileLocator = configFileLocator ?? throw new ArgumentNullException(nameof(configFileLocator));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.configInitWizard = configInitWizard ?? throw new ArgumentNullException(nameof(this.configInitWizard));
        this.repositoryInfo = repositoryInfo ?? throw new ArgumentNullException(nameof(this.repositoryInfo));
    }

    public Config Provide(Config? overrideConfig = null)
    {
        var gitVersionOptions = this.options.Value;
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = this.repositoryInfo.ProjectRootDirectory;

        var rootDirectory = this.configFileLocator.HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory;
        return Provide(rootDirectory, overrideConfig);
    }

    public Config Provide(string workingDirectory, Config? overrideConfig = null) =>
        new ConfigurationBuilder()
            .Add(this.configFileLocator.ReadConfig(workingDirectory))
            .Add(overrideConfig ?? new Config())
            .Build();

    public void Init(string workingDirectory)
    {
        var configFilePath = this.configFileLocator.GetConfigFilePath(workingDirectory);
        var currentConfiguration = this.configFileLocator.ReadConfig(workingDirectory);

        var config = this.configInitWizard.Run(currentConfiguration, workingDirectory);
        if (config == null) return;

        using var stream = this.fileSystem.OpenWrite(configFilePath);
        using var writer = new StreamWriter(stream);
        this.log.Info("Saving config file");
        ConfigSerializer.Write(config, writer);
        stream.Flush();
    }
}
