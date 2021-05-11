using System;
using System.Collections.Generic;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard
{
    public class ConfigInitWizard : IConfigInitWizard
    {
        private readonly IConsole console;
        private readonly IConfigInitStepFactory stepFactory;

        public ConfigInitWizard(IConsole console, IConfigInitStepFactory stepFactory)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        }

        public Config Run(Config config, string workingDirectory)
        {
            console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
            var steps = new Queue<ConfigInitWizardStep>();
            steps.Enqueue(stepFactory.CreateStep<EditConfigStep>());

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
}
