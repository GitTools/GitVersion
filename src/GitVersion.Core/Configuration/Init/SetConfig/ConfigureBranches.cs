using GitVersion.Configuration.Init.Wizard;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.SetConfig;

internal class ConfigureBranches : ConfigInitWizardStep
{
    public ConfigureBranches(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        if (int.TryParse(result, out var parsed))
        {
            if (parsed == 0)
            {
                steps.Enqueue(this.StepFactory.CreateStep<EditConfigStep>());
                return StepResult.Ok();
            }

            try
            {
                var configuration = configurationBuilder.Build();
                var (name, branchConfiguration) = OrderedBranches(configuration).ElementAt(parsed - 1);
                var branchConfigurationBuilder = BranchConfigurationBuilder.New.WithConfiguration(branchConfiguration);
                configurationBuilder.WithBranch(name, branchConfigurationBuilder);
                steps.Enqueue(this.StepFactory.CreateStep<ConfigureBranch>().WithData(name, branchConfigurationBuilder));
                return StepResult.Ok();
            }
            catch (ArgumentOutOfRangeException)
            { }
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        var configuration = configurationBuilder.Build();
        return @"Which branch would you like to configure:

0) Go Back
" + string.Join(PathHelper.NewLine, OrderedBranches(configuration).Select((c, i) => $"{i + 1}) {c.Key}"));
    }

    private static IOrderedEnumerable<KeyValuePair<string, IBranchConfiguration>> OrderedBranches(IGitVersionConfiguration configuration)
    {
        var defaultConfig = GitFlowConfigurationBuilder.New.Build();

        var configurationBranches = configuration.Branches;
        var defaultConfigurationBranches = defaultConfig.Branches
            .Where(k => !configurationBranches.ContainsKey(k.Key))
            // Return an empty branch configuration
            .Select(v => new KeyValuePair<string, IBranchConfiguration>(v.Key, new BranchConfiguration()));
        return configurationBranches.Union(defaultConfigurationBranches).OrderBy(b => b.Key);
    }

    protected override string DefaultResult => "0";
}
