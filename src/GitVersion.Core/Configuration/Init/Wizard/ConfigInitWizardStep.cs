using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard;

public abstract class ConfigInitWizardStep
{
    protected readonly IConsole Console;
    protected readonly IFileSystem FileSystem;
    protected readonly ILog Log;
    protected readonly IConfigInitStepFactory StepFactory;

    protected ConfigInitWizardStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
    {
        this.Console = console.NotNull();
        this.FileSystem = fileSystem.NotNull();
        this.Log = log.NotNull();
        this.StepFactory = stepFactory.NotNull();
    }

    public bool Apply(Queue<ConfigInitWizardStep> steps, GitVersionConfiguration configuration, string workingDirectory)
    {
        this.Console.WriteLine();
        this.Console.WriteLine(GetPrompt(configuration, workingDirectory));
        this.Console.WriteLine();
        this.Console.Write("> ");
        var input = this.Console.ReadLine();
        if (input == null)
        {
            this.Console.WriteLine("Would you like to save changes? (y/n)");
            input = this.Console.ReadLine();
            if (input == null || string.Equals(input, "n", StringComparison.OrdinalIgnoreCase)) return false;
            if (string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
            {
                steps.Clear();
                return true;
            }

            InvalidResponse(steps);
            return true;
        }
        var resultWithDefaultApplied = input.IsNullOrEmpty() ? DefaultResult : input;
        var stepResult = HandleResult(resultWithDefaultApplied, steps, configuration, workingDirectory);
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

    protected abstract StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, GitVersionConfiguration configuration, string workingDirectory);
    protected abstract string GetPrompt(GitVersionConfiguration configuration, string workingDirectory);
    protected abstract string? DefaultResult { get; }
}
