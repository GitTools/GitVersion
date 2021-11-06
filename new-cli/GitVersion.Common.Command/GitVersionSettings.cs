using System.IO;

namespace GitVersion.Command;

public record GitVersionSettings
{
    public const string LogFileOptionAlias1 = "--log-file";
    public const string LogFileOptionAlias2 = "-l";

    public const string WorkDirOption = "--work-dir";

    [Option(new[] { LogFileOptionAlias1, LogFileOptionAlias2 }, "The log file")]
    public FileInfo? LogFile { get; init; } = default;

    [Option(WorkDirOption, "The working directory with the git repository")]
    public DirectoryInfo WorkDir { get; init; } = new(System.Environment.CurrentDirectory);
}