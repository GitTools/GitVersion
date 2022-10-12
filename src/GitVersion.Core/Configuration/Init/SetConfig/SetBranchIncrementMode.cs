using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration.Init.SetConfig;

public class SetBranchIncrementMode : ConfigInitWizardStep
{
    private string name;
    private BranchConfiguration branchConfiguration;

    public SetBranchIncrementMode(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    public SetBranchIncrementMode WithData(string configName, BranchConfiguration branchConfiguration)
    {
        this.branchConfiguration = branchConfiguration;
        this.name = configName;
        return this;
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, GitVersionConfiguration configuration, string workingDirectory)
    {
        var configureBranchStep = this.StepFactory.CreateStep<ConfigureBranch>();
        switch (result)
        {
            case "0":
                steps.Enqueue(configureBranchStep.WithData(this.name, this.branchConfiguration));
                return StepResult.Ok();
            case "1":
                this.branchConfiguration.VersioningMode = VersioningMode.ContinuousDelivery;
                steps.Enqueue(configureBranchStep.WithData(name, this.branchConfiguration));
                return StepResult.Ok();
            case "2":
                this.branchConfiguration.VersioningMode = VersioningMode.ContinuousDeployment;
                steps.Enqueue(configureBranchStep.WithData(name, this.branchConfiguration));
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(GitVersionConfiguration configuration, string workingDirectory) => $@"What do you want the increment mode for {this.name} to be?

0) Go Back
1) Follow SemVer and only increment when a release has been tagged (continuous delivery mode)
2) Increment based on branch configuration every commit (continuous deployment mode)";

    protected override string DefaultResult => "0";
}
