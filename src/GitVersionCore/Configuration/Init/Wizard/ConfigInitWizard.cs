using System.Collections.Generic;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard
{
    public class ConfigInitWizard
    {
        readonly IConsole console;
        readonly IFileSystem fileSystem;
        private readonly ILog log;

        public ConfigInitWizard(IConsole console, IFileSystem fileSystem, ILog log)
        {
            this.console = console;
            this.fileSystem = fileSystem;
            this.log = log;
        }

        public Config Run(Config config, string workingDirectory)
        {
            console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
            var steps = new Queue<ConfigInitWizardStep>();
            steps.Enqueue(new EditConfigStep(console, fileSystem, log));

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
