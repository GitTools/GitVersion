using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.BuildServer;

internal class AppveyorPublicPrivate : ConfigInitWizardStep
{
    public AppveyorPublicPrivate(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, GitVersionConfiguration configuration, string workingDirectory)
    {
        switch (result)
        {
            case "0":
                steps.Enqueue(this.StepFactory.CreateStep<EditConfigStep>());
                return StepResult.Ok();
            case "1":
                steps.Enqueue(this.StepFactory.CreateStep<AppVeyorSetup>().WithData(ProjectVisibility.Public));
                return StepResult.Ok();
            case "2":
                steps.Enqueue(this.StepFactory.CreateStep<AppVeyorSetup>().WithData(ProjectVisibility.Private));
                return StepResult.Ok();
        }
        return StepResult.Ok();
    }

    protected override string GetPrompt(GitVersionConfiguration configuration, string workingDirectory) => @"Is your project public or private?

That is ... does it require authentication to clone/pull?

0) Go Back
1) Public
2) Private";

    protected override string DefaultResult => "0";
}
