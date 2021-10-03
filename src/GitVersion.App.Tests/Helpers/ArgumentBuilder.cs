using GitVersion.Extensions;

namespace GitVersion.App.Tests;

public class ArgumentBuilder
{
    public ArgumentBuilder(string workingDirectory) => this.workingDirectory = workingDirectory;

    public ArgumentBuilder(string workingDirectory, string exec, string execArgs, string projectFile, string projectArgs, string logFile)
    {
        this.workingDirectory = workingDirectory;
        this.exec = exec;
        this.execArgs = execArgs;
        this.projectFile = projectFile;
        this.projectArgs = projectArgs;
        this.logFile = logFile;
    }

    public ArgumentBuilder(string workingDirectory, string additionalArguments, string logFile)
    {
        this.workingDirectory = workingDirectory;
        this.additionalArguments = additionalArguments;
        this.logFile = logFile;
    }

    public string WorkingDirectory => this.workingDirectory;

    public string LogFile => this.logFile;

    public override string ToString()
    {
        var arguments = new StringBuilder();

        arguments.AppendFormat(" /targetpath \"{0}\"", this.workingDirectory);

        if (!this.exec.IsNullOrWhiteSpace())
        {
            arguments.AppendFormat(" /exec \"{0}\"", this.exec);
        }

        if (!this.execArgs.IsNullOrWhiteSpace())
        {
            arguments.AppendFormat(" /execArgs \"{0}\"", this.execArgs);
        }

        if (!this.projectFile.IsNullOrWhiteSpace())
        {
            arguments.AppendFormat(" /proj \"{0}\"", this.projectFile);
        }

        if (!this.projectArgs.IsNullOrWhiteSpace())
        {
            arguments.AppendFormat(" /projargs \"{0}\"", this.projectArgs);
        }

        if (!this.logFile.IsNullOrWhiteSpace())
        {
            arguments.AppendFormat(" /l \"{0}\"", this.logFile);
        }

        arguments.Append(this.additionalArguments);

        return arguments.ToString();
    }

    private readonly string additionalArguments;
    private readonly string exec;
    private readonly string execArgs;
    private readonly string logFile;
    private readonly string projectArgs;
    private readonly string projectFile;
    private readonly string workingDirectory;
}
