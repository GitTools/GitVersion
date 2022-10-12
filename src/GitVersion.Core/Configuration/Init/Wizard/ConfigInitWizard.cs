using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard;

public class ConfigInitWizard : IConfigInitWizard
{
    private readonly IConsole console;
    private readonly IConfigInitStepFactory stepFactory;

    public ConfigInitWizard(IConsole console, IConfigInitStepFactory stepFactory)
    {
        this.console = console.NotNull();
        this.stepFactory = stepFactory.NotNull();
    }

    public GitVersionConfiguration? Run(GitVersionConfiguration configuration, string workingDirectory)
    {
        this.console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
        var steps = new Queue<ConfigInitWizardStep>();
        steps.Enqueue(this.stepFactory.CreateStep<EditConfigStep>());

        while (steps.Count > 0)
        {
            var currentStep = steps.Dequeue();
            if (!currentStep.Apply(steps, configuration, workingDirectory))
            {
                return null;
            }
        }

        return configuration;
    }
}
