using GitVersion.Configuration.SupportedWorkflows;
using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Core;

namespace GitVersion.Configuration;

internal class ConfigurationProvider(
    IFileSystem fileSystem,
    ILog log,
    IConfigurationFileLocator configFileLocator,
    IOptions<GitVersionOptions> options)
    : IConfigurationProvider
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly ILog log = log.NotNull();
    private readonly IConfigurationFileLocator configFileLocator = configFileLocator.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public IGitVersionConfiguration Provide(IReadOnlyDictionary<object, object?>? overrideConfiguration)
    {
        var gitVersionOptions = this.options.Value;
        var workingDirectory = gitVersionOptions.WorkingDirectory;
        var projectRootDirectory = workingDirectory.FindGitDir()?.WorkingTreeDirectory;

        return this.configFileLocator.TryGetConfigurationFile(workingDirectory, projectRootDirectory, out var configFilePath)
            ? ProvideConfiguration(configFilePath, overrideConfiguration)
            : ProvideForDirectory(null, overrideConfiguration);
    }

    internal IGitVersionConfiguration ProvideForDirectory(string? workingDirectory,
                                                         IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        this.configFileLocator.TryGetConfigurationFile(workingDirectory, null, out var configFilePath);
        return ProvideConfiguration(configFilePath, overrideConfiguration);
    }

    private IGitVersionConfiguration ProvideConfiguration(string? configFile,
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
            var baseException = exception.GetBaseException();
            throw new WarningException(
                $"Could not build the configuration instance because following exception occurred: '{baseException.Message}' " +
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
