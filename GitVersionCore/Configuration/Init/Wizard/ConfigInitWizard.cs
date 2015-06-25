namespace GitVersion.Configuration.Init.Wizard
{
    using System;
    using System.Collections.Generic;
    using GitVersion.Helpers;

    public class ConfigInitWizard
    {
        public Config Run(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            Console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
            var steps = new Queue<ConfigInitWizardStep>();
            steps.Enqueue(new EditConfigStep());

            while (steps.Count > 0)
            {
                var currentStep = steps.Dequeue();
                if (!currentStep.Apply(steps, config, workingDirectory, fileSystem))
                {
                    return null;
                }
            }

            return config;
        }
    }
}