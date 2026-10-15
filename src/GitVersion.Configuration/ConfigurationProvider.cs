using System.IO.Abstractions;
using GitVersion.Extensions;

namespace GitVersion.Configuration;

internal class ConfigurationProvider(
    IConfigurationFileLocator configFileLocator,
    IFileSystem fileSystem,
    ILogger<ConfigurationProvider> logger,
    IConfigurationSerializer configurationSerializer,
    IOptions<GitVersionOptions> options,
    ConfigurationResolver resolver)
    : IConfigurationProvider
{
    private readonly IConfigurationFileLocator configFileLocator = configFileLocator.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly ILogger<ConfigurationProvider> logger = logger.NotNull();
    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private bool legacyConfigurationWarningLogged;

    public IGitVersionConfiguration Provide(IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        var gitVersionOptions = this.options.Value;
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = this.fileSystem.FindGitDir(workingDirectory)?.WorkingTreeDirectory;

        var configurationFile = this.configFileLocator.GetConfigurationFile(workingDirectory)
                             ?? this.configFileLocator.GetConfigurationFile(projectRootDirectory);

        return configurationFile is not null
            ? ProvideConfiguration(configurationFile, overrideConfiguration)
            : ProvideForDirectory(null, overrideConfiguration);
    }

    internal IGitVersionConfiguration ProvideForDirectory(string? workingDirectory,
                                                          IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        var configFilePath = this.configFileLocator.GetConfigurationFile(workingDirectory);
        return ProvideConfiguration(configFilePath, overrideConfiguration);
    }

    private IGitVersionConfiguration ProvideConfiguration(string? configFile,
                                                          IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        var configurationVersion = ConfigurationVersionSelector.Resolve();
        this.logger.LogInformation("Configuration version: {ConfigurationVersion}", configurationVersion == ConfigurationVersion.V6 ? "v6" : "v7");
        var configurationFromFile = ReadOverrideConfiguration(configFile);
        var configuration = resolver.Resolve(configurationFromFile is null ? null : ConfigurationSerializer.SerializeLegacy(configurationFromFile), overrideConfiguration);
        WarnAboutExplicitLegacyConfiguration(configFile, configurationFromFile);
        return configuration;
    }

    private Dictionary<object, object?>? ReadOverrideConfiguration(string? configFilePath)
    {
        if (configFilePath == null)
        {
            this.logger.LogInformation("No configuration file found, using default configuration");
            return null;
        }

        if (!this.fileSystem.File.Exists(configFilePath))
        {
            this.logger.LogInformation("Configuration file '{ConfigFilePath}' not found", configFilePath);
            return null;
        }

        this.logger.LogInformation("Using configuration file '{ConfigFilePath}'", configFilePath);
        var content = this.fileSystem.File.ReadAllText(configFilePath);
        return this.configurationSerializer.Deserialize<Dictionary<object, object?>>(content);
    }

    private void WarnAboutExplicitLegacyConfiguration(string? configFilePath, Dictionary<object, object?>? configuration)
    {
        if (configuration is null || this.legacyConfigurationWarningLogged || !ConfigurationVersionSelector.IsExplicitV6())
        {
            return;
        }

        this.legacyConfigurationWarningLogged = true;
        this.logger.LogWarning(
            "Configuration file '{ConfigurationFile}' uses the temporary v6 compatibility mode. Legacy configuration loading is removed in GitVersion 7.1. " +
            "Run 'gitversion config migrate' and validate with {ConfigurationVersion}=v7.",
            configFilePath,
            ConfigurationVersionSelector.EnvironmentVariableName);
    }

}
