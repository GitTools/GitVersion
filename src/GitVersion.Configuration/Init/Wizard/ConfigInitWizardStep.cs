using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard;

internal abstract class ConfigInitWizardStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
{
    protected readonly IConsole Console = console.NotNull();
    protected readonly IFileSystem FileSystem = fileSystem.NotNull();
    protected readonly ILog Log = log.NotNull();
    protected readonly IConfigInitStepFactory StepFactory = stepFactory.NotNull();

    public bool Apply(Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        this.Console.WriteLine();
        this.Console.WriteLine(GetPrompt(configurationBuilder, workingDirectory));
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
        var stepResult = HandleResult(resultWithDefaultApplied, steps, configurationBuilder, workingDirectory);
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

    protected abstract StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory);
    protected abstract string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory);
    protected abstract string? DefaultResult { get; }
}
