using GitVersion.Extensions;

namespace GitVersion.App.Tests;

public class ArgumentBuilder
{
    public ArgumentBuilder(string workingDirectory) => this.WorkingDirectory = workingDirectory;

    public ArgumentBuilder(string workingDirectory, string? exec, string? execArgs, string? projectFile, string? projectArgs, string? logFile)
    {
        this.WorkingDirectory = workingDirectory;
        this.exec = exec;
        this.execArgs = execArgs;
        this.projectFile = projectFile;
        this.projectArgs = projectArgs;
        this.LogFile = logFile;
    }

    public ArgumentBuilder(string workingDirectory, string? additionalArguments, string? logFile)
    {
        this.WorkingDirectory = workingDirectory;
        this.additionalArguments = additionalArguments;
        this.LogFile = logFile;
    }

    public string WorkingDirectory { get; }

    public string? LogFile { get; }

    public override string ToString()
    {
        var arguments = new StringBuilder();

        arguments.Append(" /targetpath \"").Append(this.WorkingDirectory).Append('\"');

        if (!this.exec.IsNullOrWhiteSpace())
        {
            arguments.Append(" /exec \"").Append(this.exec).Append('\"');
        }

        if (!this.execArgs.IsNullOrWhiteSpace())
        {
            arguments.Append(" /execArgs \"").Append(this.execArgs).Append('\"');
        }

        if (!this.projectFile.IsNullOrWhiteSpace())
        {
            arguments.Append(" /proj \"").Append(this.projectFile).Append('\"');
        }

        if (!this.projectArgs.IsNullOrWhiteSpace())
        {
            arguments.Append(" /projargs \"").Append(this.projectArgs).Append('\"');
        }

        if (!this.LogFile.IsNullOrWhiteSpace())
        {
            arguments.Append(" /l \"").Append(this.LogFile).Append('\"');
        }

        arguments.Append(this.additionalArguments);

        return arguments.ToString();
    }

    private readonly string? additionalArguments;
    private readonly string? exec;
    private readonly string? execArgs;
    private readonly string? projectArgs;
    private readonly string? projectFile;
}
