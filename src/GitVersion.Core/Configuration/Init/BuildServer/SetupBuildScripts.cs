using GitVersion.Configurations.Init.Wizard;
using GitVersion.Logging;

namespace GitVersion.Configurations.Init.BuildServer;

internal class SetupBuildScripts : ConfigInitWizardStep
{
    public SetupBuildScripts(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, Model.Configurations.Configuration configuration, string workingDirectory)
    {
        switch (result)
        {
            case "0":
                steps.Enqueue(this.StepFactory.CreateStep<EditConfigStep>());
                return StepResult.Ok();
            case "1":
                steps.Enqueue(this.StepFactory.CreateStep<AppveyorPublicPrivate>());
                return StepResult.Ok();
        }
        return StepResult.Ok();
    }

    protected override string GetPrompt(Model.Configurations.Configuration configuration, string workingDirectory) => @"What build server are you using?

Want to see more? Contribute a pull request!

0) Go Back
1) AppVeyor";

    protected override string DefaultResult => "0";
}
