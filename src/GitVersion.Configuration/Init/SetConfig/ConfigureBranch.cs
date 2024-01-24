using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.SetConfig;

internal class ConfigureBranch(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
    : ConfigInitWizardStep(console, fileSystem, log, stepFactory)
{
    private string name;
    private BranchConfigurationBuilder branchConfigurationBuilder;

    public ConfigureBranch WithData(string configName, BranchConfigurationBuilder configurationBuilder)
    {
        this.branchConfigurationBuilder = configurationBuilder;
        this.name = configName;
        return this;
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        switch (result)
        {
            case "0":
                steps.Enqueue(this.StepFactory.CreateStep<ConfigureBranches>());
                return StepResult.Ok();
            case "1":
                steps.Enqueue(this.StepFactory.CreateStep<SetBranchTag>().WithData(name, this.branchConfigurationBuilder));
                return StepResult.Ok();
            case "2":
                steps.Enqueue(this.StepFactory.CreateStep<SetBranchIncrementMode>().WithData(name, this.branchConfigurationBuilder));
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        var branchConfiguration = this.branchConfigurationBuilder.Build();
        return $@"What would you like to change for '{this.name}':

0) Go Back
1) Branch Pr-release tag (Current: {branchConfiguration.Label})
2) Branch Increment mode (per commit/after tag) (Current: {branchConfiguration.DeploymentMode})";
    }

    protected override string DefaultResult => "0";
}
