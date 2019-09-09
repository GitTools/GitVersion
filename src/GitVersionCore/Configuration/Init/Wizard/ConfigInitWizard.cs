using System.Collections.Generic;
using GitVersion.Helpers;

namespace GitVersion.Configuration.Init.Wizard
{
    public class ConfigInitWizard
    {
        readonly IConsole console;
        readonly IFileSystem fileSystem;

        public ConfigInitWizard(IConsole console, IFileSystem fileSystem)
        {
            this.console = console;
            this.fileSystem = fileSystem;
        }

        public Config Run(Config config, string workingDirectory)
        {
            console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
            var steps = new Queue<ConfigInitWizardStep>();
            steps.Enqueue(new EditConfigStep(console, fileSystem));

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