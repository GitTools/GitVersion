using System;
using System.Collections.Generic;
using GitVersion.Helpers;

namespace GitVersion.Configuration.Init.Wizard
{
    public abstract class ConfigInitWizardStep
    {
        protected ConfigInitWizardStep(IConsole console, IFileSystem fileSystem)
        {
            Console = console;
            FileSystem = fileSystem;
        }

        protected IConsole Console { get; private set; }

        protected IFileSystem FileSystem { get; private set; }

        public bool Apply(Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            Console.WriteLine();
            Console.WriteLine(GetPrompt(config, workingDirectory));
            Console.WriteLine();
            Console.Write("> ");
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
            var stepResult = HandleResult(resultWithDefaultApplied, steps, config, workingDirectory);
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
            using (Console.UseColor(ConsoleColor.Red))
            {

            }
            Console.WriteLine("Invalid response!");
            steps.Enqueue(this);
        }

        protected abstract StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory);
        protected abstract string GetPrompt(Config config, string workingDirectory);
        protected abstract string DefaultResult { get; }
    }
}