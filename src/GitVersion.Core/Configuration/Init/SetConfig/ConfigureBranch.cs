using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.SetConfig;

public class ConfigureBranch : ConfigInitWizardStep
{
    private string name;
    private BranchConfiguration branchConfiguration;

    public ConfigureBranch(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    public ConfigureBranch WithData(string configName, BranchConfiguration configuration)
    {
        this.branchConfiguration = configuration;
        this.name = configName;
        return this;
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, GitVersionConfiguration configuration, string workingDirectory)
    {
        switch (result)
        {
            case "0":
                steps.Enqueue(this.StepFactory.CreateStep<ConfigureBranches>());
                return StepResult.Ok();
            case "1":
                steps.Enqueue(this.StepFactory.CreateStep<SetBranchTag>().WithData(name, branchConfiguration));
                return StepResult.Ok();
            case "2":
                steps.Enqueue(this.StepFactory.CreateStep<SetBranchIncrementMode>().WithData(name, branchConfiguration));
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(GitVersionConfiguration configuration, string workingDirectory) => $@"What would you like to change for '{this.name}':

0) Go Back
1) Branch Pr-release tag (Current: {this.branchConfiguration.Tag})
2) Branch Increment mode (per commit/after tag) (Current: {this.branchConfiguration.VersioningMode})";

    protected override string DefaultResult => "0";
}
