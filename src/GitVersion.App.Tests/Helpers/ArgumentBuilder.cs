using GitVersion.Extensions;

namespace GitVersion.App.Tests;

public class ArgumentBuilder(string? workingDirectory, string? additionalArguments, string? logFile)
{
    public string? WorkingDirectory { get; } = workingDirectory;

    public string? LogFile { get; } = logFile;

    public override string ToString()
    {
        var arguments = new StringBuilder();

        if (!this.WorkingDirectory.IsNullOrWhiteSpace())
        {
            arguments.Append(" --target-path \"").Append(this.WorkingDirectory).Append('\"');
        }

        if (!this.LogFile.IsNullOrWhiteSpace())
        {
            arguments.Append(" --log-file \"").Append(this.LogFile).Append('\"');
        }

        arguments.Append(additionalArguments);

        return arguments.ToString();
    }
}
