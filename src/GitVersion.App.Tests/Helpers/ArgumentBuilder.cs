using GitVersion.Extensions;

namespace GitVersion.App.Tests;

public class ArgumentBuilder(string? workingDirectory, string? additionalArguments, string? logFile)
{
    public string? WorkingDirectory { get; } = workingDirectory;

    public string? LogFile { get; } = logFile;

    public override string ToString()
    {
        var arguments = new StringBuilder();

        if (!WorkingDirectory.IsNullOrWhiteSpace())
        {
            arguments.Append(" /targetpath \"").Append(WorkingDirectory).Append('\"');
        }

        if (!LogFile.IsNullOrWhiteSpace())
        {
            arguments.Append(" /l \"").Append(LogFile).Append('\"');
        }

        arguments.Append(additionalArguments);

        return arguments.ToString();
    }
}
