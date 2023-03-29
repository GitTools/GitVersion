using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration.Init.SetConfig;

internal class GlobalModeSetting : ConfigInitWizardStep
{
    private ConfigInitWizardStep returnToStep;
    private bool isPartOfWizard;

    protected GlobalModeSetting(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    public GlobalModeSetting WithData(ConfigInitWizardStep returnStep, bool isPartOfTheWizard)
    {
        this.returnToStep = returnStep;
        this.isPartOfWizard = isPartOfTheWizard;
        return this;
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        switch (result)
        {
            case "1":
                configurationBuilder.WithVersioningMode(VersioningMode.ContinuousDelivery);
                steps.Enqueue(this.returnToStep);
                return StepResult.Ok();
            case "2":
                configurationBuilder.WithVersioningMode(VersioningMode.ContinuousDeployment);
                steps.Enqueue(this.returnToStep);
                return StepResult.Ok();
            case "3":
                configurationBuilder.WithVersioningMode(VersioningMode.Mainline);
                steps.Enqueue(this.returnToStep);
                return StepResult.Ok();
            case "0":
            case "4":
                steps.Enqueue(this.returnToStep);
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory) => $@"What do you want the default increment mode to be (can be override per branch):
{(!this.isPartOfWizard ? "0) Go Back" : string.Empty)}
1) Follow SemVer and only increment when a release has been tagged (continuous delivery mode)
2) Increment based on branch configuration every commit (continuous deployment mode)
3) Each merged branch against main will increment the version (mainline mode)
{(this.isPartOfWizard ? "4) Skip" : string.Empty)}";

    protected override string DefaultResult => "4";
}
