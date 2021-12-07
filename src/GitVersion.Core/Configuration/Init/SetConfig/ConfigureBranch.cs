using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.SetConfig;

public class ConfigureBranch : ConfigInitWizardStep
{
    private string? name;
    private BranchConfig? branchConfig;

    public ConfigureBranch(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    public ConfigureBranch WithData(string? name, BranchConfig? branchConfig)
    {
        this.branchConfig = branchConfig;
        this.name = name;
        return this;
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
    {
        switch (result)
        {
            case "0":
                steps.Enqueue(this.StepFactory.CreateStep<ConfigureBranches>()!);
                return StepResult.Ok();
            case "1":
                steps.Enqueue(this.StepFactory.CreateStep<SetBranchTag>()!.WithData(name, branchConfig));
                return StepResult.Ok();
            case "2":
                steps.Enqueue(this.StepFactory.CreateStep<SetBranchIncrementMode>()!.WithData(name!, branchConfig!));
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(Config config, string workingDirectory) => $@"What would you like to change for '{this.name}':

0) Go Back
1) Branch Pre-release tag (Current: {this.branchConfig!.Tag})
2) Branch Increment mode (per commit/after tag) (Current: {this.branchConfig.VersioningMode})";

    protected override string DefaultResult => "0";
}
