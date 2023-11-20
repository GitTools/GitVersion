using GitVersion.Configuration.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init;

internal class SetNextVersion : ConfigInitWizardStep
{
    public SetNextVersion(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        var editConfigStep = this.StepFactory.CreateStep<EditConfigStep>();
        if (result.IsNullOrEmpty())
        {
            steps.Enqueue(editConfigStep);
            return StepResult.Ok();
        }

        var configuration = configurationBuilder.Build();
        if (!SemanticVersion.TryParse(result, string.Empty, out var semVer, configuration.SemanticVersionFormat))
            return StepResult.InvalidResponseSelected();

        configurationBuilder.WithNextVersion(semVer.ToString("t"));
        steps.Enqueue(editConfigStep);
        return StepResult.Ok();
    }

    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory) => "What would you like to set the next version to (enter nothing to cancel)?";

    protected override string? DefaultResult => null;
}
