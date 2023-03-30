using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard;

internal class ConfigInitWizard : IConfigInitWizard
{
    private readonly IConsole console;
    private readonly IConfigInitStepFactory stepFactory;

    public ConfigInitWizard(IConsole console, IConfigInitStepFactory stepFactory)
    {
        this.console = console.NotNull();
        this.stepFactory = stepFactory.NotNull();
    }

    public IGitVersionConfiguration? Run(IGitVersionConfiguration configuration, string workingDirectory)
    {
        this.console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
        var steps = new Queue<ConfigInitWizardStep>();
        steps.Enqueue(this.stepFactory.CreateStep<EditConfigStep>());

        var configurationBuilder = ConfigurationBuilder.New.WithConfiguration(configuration);
        while (steps.Count > 0)
        {
            var currentStep = steps.Dequeue();
            if (!currentStep.Apply(steps, configurationBuilder, workingDirectory))
            {
                return null;
            }
        }

        return configurationBuilder.Build();
    }
}
