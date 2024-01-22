using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard;

internal class PickBranchingStrategy1Step(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
    : ConfigInitWizardStep(console, fileSystem, log, stepFactory)
{
    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        switch (result?.ToLower())
        {
            case "y":
                this.Console.Write(@"Because you need to maintain multiple versions of your product in production at the same time, GitFlow is likely a good fit.

GitFlow allows you to have new development happening on the 'develop' branch, patch issues in old minor versions with 'hotfix/' branches and support old major versions with 'support/' branches");
                steps.Enqueue(this.StepFactory.CreateStep<PickBranchingStrategyStep>());
                return StepResult.Ok();
            case "n":
                steps.Enqueue(this.StepFactory.CreateStep<PickBranchingStrategy2Step>());
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory) => @"GitVersion can try to recommend you a branching strategy based on a few questions.

Do you need to maintain multiple versions of your application simultaneously in production? (y/n)";

    protected override string? DefaultResult => null;
}
