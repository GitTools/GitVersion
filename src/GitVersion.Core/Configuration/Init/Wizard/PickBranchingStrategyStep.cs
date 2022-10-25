using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard;

public class PickBranchingStrategyStep : ConfigInitWizardStep
{
    public PickBranchingStrategyStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, GitVersionConfiguration configuration, string workingDirectory)
    {
        var returnToStep = this.StepFactory.CreateStep<FinishedSetupStep>();
        switch (result)
        {
            case "1":
                steps.Enqueue(this.StepFactory.CreateStep<GitFlowSetupStep>().WithData(returnToStep, true));
                break;
            case "2":
                steps.Enqueue(this.StepFactory.CreateStep<GitHubFlowStep>().WithData(returnToStep, true));
                break;
            case "3":
                steps.Enqueue(this.StepFactory.CreateStep<PickBranchingStrategy1Step>());
                break;
            default:
                return StepResult.InvalidResponseSelected();
        }

        return StepResult.Ok();
    }

    protected override string GetPrompt(GitVersionConfiguration configuration, string workingDirectory) => @"The way you will use GitVersion will change a lot based on your branching strategy. What branching strategy will you be using:

1) GitFlow (or similar)
2) GitHubFlow
3) Unsure, tell me more";

    protected override string? DefaultResult => null;
}
