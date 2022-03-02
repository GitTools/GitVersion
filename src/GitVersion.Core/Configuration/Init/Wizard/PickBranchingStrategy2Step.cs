using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard;

public class PickBranchingStrategy2Step : ConfigInitWizardStep
{
    public PickBranchingStrategy2Step(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
    {
        switch (result?.ToLower())
        {
            case "y":
                this.Console.WriteLine("GitFlow is likely a good fit, the 'develop' branch can be used " +
                                       "for active development while stabilising the next release.");
                this.Console.WriteLine();
                this.Console.WriteLine("GitHubFlow is designed for a lightweight workflow where main is always " +
                                       "good to deploy to production and feature branches are used to stabilise " +
                                       "features, once stable they are merged to main and made available in the next release");
                steps.Enqueue(this.StepFactory.CreateStep<PickBranchingStrategyStep>()!);
                return StepResult.Ok();
            case "n":
                steps.Enqueue(this.StepFactory.CreateStep<PickBranchingStrategy3Step>()!);
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(Config config, string workingDirectory) => "Do you stabilise releases while continuing work on the next version? (y/n)";

    protected override string? DefaultResult => null;
}
