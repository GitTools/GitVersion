using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard;

public abstract class ConfigInitWizardStep
{
    protected readonly IConsole Console;
    protected readonly IFileSystem FileSystem;
    protected readonly ILog Log;
    protected readonly IConfigInitStepFactory StepFactory;

    protected ConfigInitWizardStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
    {
        this.Console = console ?? throw new ArgumentNullException(nameof(console));
        this.FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.Log = log ?? throw new ArgumentNullException(nameof(log));
        this.StepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
    }

    public bool Apply(Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
    {
        this.Console.WriteLine();
        this.Console.WriteLine(GetPrompt(config, workingDirectory));
        this.Console.WriteLine();
        this.Console.Write("> ");
        var input = this.Console.ReadLine();
        if (input == null)
        {
            this.Console.WriteLine("Would you like to save changes? (y/n)");
            input = this.Console.ReadLine();
            if (input == null || input.ToLower() == "n") return false;
            if (input.ToLower() == "y")
            {
                steps.Clear();
                return true;
            }

            InvalidResponse(steps);
            return true;
        }
        var resultWithDefaultApplied = input.IsNullOrEmpty() ? DefaultResult : input;
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

    private void InvalidResponse(Queue<ConfigInitWizardStep> steps)
    {
        this.Console.WriteLine();
        using (this.Console.UseColor(ConsoleColor.Red))
        {
            this.Console.WriteLine("Invalid response!");
        }
        steps.Enqueue(this);
    }

    protected abstract StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory);
    protected abstract string GetPrompt(Config config, string workingDirectory);
    protected abstract string? DefaultResult { get; }
}
