using GitVersion.Configurations.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configurations.Init;

public class SetNextVersion : ConfigInitWizardStep
{
    public SetNextVersion(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, Model.Configurations.Configuration configuration, string workingDirectory)
    {
        var editConfigStep = this.StepFactory.CreateStep<EditConfigStep>();
        if (result.IsNullOrEmpty())
        {
            steps.Enqueue(editConfigStep);
            return StepResult.Ok();
        }

        if (!SemanticVersion.TryParse(result, string.Empty, out var semVer))
            return StepResult.InvalidResponseSelected();

        configuration.NextVersion = semVer.ToString("t");
        steps.Enqueue(editConfigStep);
        return StepResult.Ok();
    }

    protected override string GetPrompt(Model.Configurations.Configuration configuration, string workingDirectory) => "What would you like to set the next version to (enter nothing to cancel)?";

    protected override string? DefaultResult => null;
}
