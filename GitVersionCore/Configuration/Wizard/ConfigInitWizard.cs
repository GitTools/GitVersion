namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public class ConfigInitWizard
    {
        public Config Run(Config config)
        {
            Console.WriteLine("GitVersion init will guide you through setting GitVersion up to work for you");
            var steps = new Queue<ConfigInitWizardStep>();
            steps.Enqueue(new SimpleOrTutorialStep());

            while (steps.Count > 0)
            {
                var currentStep = steps.Dequeue();
                if (!currentStep.Apply(steps, config))
                {
                    return null;
                }
            }

            return config;
        }
    }
}