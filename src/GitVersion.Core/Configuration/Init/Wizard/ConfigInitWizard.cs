using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configurations.Init.Wizard;

public class ConfigInitWizard : IConfigInitWizard
{
    private readonly IConsole console;
    private readonly IConfigInitStepFactory stepFactory;

    public ConfigInitWizard(IConsole console, IConfigInitStepFactory stepFactory)
    {
        this.console = console.NotNull();
        this.stepFactory = stepFactory.NotNull();
    }

    public Model.Configurations.Configuration? Run(Model.Configurations.Configuration config, string workingDirectory)
    {
        this.console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
        var steps = new Queue<ConfigInitWizardStep>();
        steps.Enqueue(this.stepFactory.CreateStep<EditConfigStep>());

        while (steps.Count > 0)
        {
            var currentStep = steps.Dequeue();
            if (!currentStep.Apply(steps, config, workingDirectory))
            {
                return null;
            }
        }

        return config;
    }
}
