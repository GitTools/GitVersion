namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public abstract class ConfigInitWizardStep
    {
        public bool Apply(Queue<ConfigInitWizardStep> steps, Config config)
        {
            Console.WriteLine();
            Console.WriteLine(GetPrompt(config));
            var input = Console.ReadLine();
            if (input == null)
            {
                Console.WriteLine("Would you like to save changes? (y/n)");
                input = Console.ReadLine();
                if (input == null || input.ToLower() == "n") return false;
                if (input.ToLower() == "y")
                {
                    steps.Clear();
                    return true;
                }

                InvalidResponse(steps);
                return true;
            }
            var resultWithDefaultApplied = string.IsNullOrEmpty(input) ? DefaultResult : input;
            var stepResult = HandleResult(resultWithDefaultApplied, steps, config);
            if (stepResult.InvalidResponse)
            {
                InvalidResponse(steps);
            }
            else if (stepResult.Exit)
            {
                steps.Clear();
                return stepResult.Save;
            }
            return true;
        }

        void InvalidResponse(Queue<ConfigInitWizardStep> steps)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid response!");
            Console.ResetColor();
            steps.Enqueue(this);
        }

        protected abstract StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config);
        protected abstract string GetPrompt(Config config);
        protected abstract string DefaultResult { get; }
    }
}