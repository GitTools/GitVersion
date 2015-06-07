namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public abstract class ConfigInitWizardStep
    {
        public bool Apply(Queue<ConfigInitWizardStep> steps, Config config)
        {
            Console.WriteLine(GetPrompt(config));
            var input = Console.ReadLine();
            if (input == null)
            {
                return false;
            }
            var resultWithDefaultApplied = string.IsNullOrEmpty(input) ? DefaultResult : input;
            var stepResult = HandleResult(resultWithDefaultApplied, steps, config);
            if (stepResult.InvalidResponse)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid response!");
                Console.ResetColor();
                steps.Enqueue(this);
            }
            return true;
        }

        protected abstract StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config);
        protected abstract string GetPrompt(Config config);
        protected abstract string DefaultResult { get; }
    }
}