using GitVersion.Infrastructure;

namespace GitVersion.Command;

public class GitVersionSettings
{
    public const string LogFileOptionAlias1 = "--log-file";
    public const string LogFileOptionAlias2 = "-l";

    public const string VerbosityOption = "--verbosity";
    public const string WorkDirOption = "--work-dir";

    [Option(new[] { LogFileOptionAlias1, LogFileOptionAlias2 }, "The log file")]
    public FileInfo? LogFile { get; init; } = default;

    [Option(VerbosityOption, "The verbosity of the logging information")]
    public Verbosity Verbosity { get; init; } = Verbosity.Normal;
    
    [Option(WorkDirOption, "The working directory with the git repository")]
    public DirectoryInfo WorkDir { get; init; } = new(Environment.CurrentDirectory);
}