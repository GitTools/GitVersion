using GitVersion.Configuration.Init.Wizard;
using GitVersion.Configuration.SupportedWorkflows;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Core;

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

    public GitVersionConfiguration Provide(IReadOnlyDictionary<object, object?>? overrideConfiguration)
    {
        var gitVersionOptions = this.options.Value;
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = workingDirectory.FindGitDir()?.WorkingTreeDirectory;

        return this.configFileLocator.TryGetConfigurationFile(workingDirectory, projectRootDirectory, out var configFilePath)
            ? ProvideConfiguration(configFilePath, overrideConfiguration)
            : ProvideForDirectory(null, overrideConfiguration);
    }

    public void Init(string workingDirectory)
    {
        var gitVersionOptions = this.options.Value;
        var fileName = gitVersionOptions.ConfigurationInfo.ConfigurationFile ?? ConfigurationFileLocator.DefaultFileName;
        var configFilePath = PathHelper.Combine(workingDirectory, fileName);
        var currentConfiguration = this.configFileLocator.ReadConfiguration(configFilePath);

        var configuration = this.configInitWizard.Run(currentConfiguration, workingDirectory);
        if (configuration == null) return;

        using var stream = this.fileSystem.OpenWrite(configFilePath);
        using var writer = new StreamWriter(stream);
        this.log.Info("Saving configuration file");
        ConfigurationSerializer.Write(configuration, writer);
        stream.Flush();
    }

    internal GitVersionConfiguration ProvideForDirectory(string? workingDirectory,
                                                         IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        this.configFileLocator.TryGetConfigurationFile(workingDirectory, null, out var configFilePath);
        return ProvideConfiguration(configFilePath, overrideConfiguration);
    }

    private GitVersionConfiguration ProvideConfiguration(string? configFile,
                                                         IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        var overrideConfigurationFromFile = this.configFileLocator.ReadOverrideConfiguration(configFile);

        var workflow = GetWorkflow(overrideConfiguration, overrideConfigurationFromFile);

        IConfigurationBuilder configurationBuilder = (workflow is null)
            ? GitFlowConfigurationBuilder.New
            : ConfigurationBuilder.New;

        var overrideConfigurationFromWorkflow = WorkflowManager.GetOverrideConfiguration(workflow);
        foreach (var item in new[] { overrideConfigurationFromWorkflow, overrideConfigurationFromFile, overrideConfiguration })
        {
            if (item != null) configurationBuilder.AddOverride(item);
        }

        try
        {
            return configurationBuilder.Build();
        }
        catch (YamlException exception)
        {
            throw new WarningException(
                $"Could not build the configuration instance because following exception occurred: '{exception.Message}' " +
                "Please ensure that the /overrideconfig parameters are correct and the configuration file is in the correct format."
            );
        }
    }

    private static string? GetWorkflow(IReadOnlyDictionary<object, object?>? overrideConfiguration, IReadOnlyDictionary<object, object?>? overrideConfigurationFromFile)
    {
        string? workflow = null;
        foreach (var item in new[] { overrideConfigurationFromFile, overrideConfiguration })
        {
            if (item?.TryGetValue("workflow", out object? value) == true && value != null)
            {
                workflow = (string)value;
            }
        }

        return workflow;
    }
}
