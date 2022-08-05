using GitVersion.Infrastructure;

namespace GitVersion;

public record GitVersionSettings
{
    public const string LogFileOptionAlias1 = "--log-file";
    private const string LogFileOptionAlias2 = "-l";

    public const string VerbosityOption = "--verbosity";
    private const string WorkDirOption = "--work-dir";

    [Option(new[] { LogFileOptionAlias1, LogFileOptionAlias2 }, "The log file")]
    public FileInfo? LogFile { get; init; }

    [Option(VerbosityOption, "The verbosity of the logging information")]
    public Verbosity Verbosity { get; init; } = Verbosity.Normal;

    [Option(WorkDirOption, "The working directory with the git repository")]
    public DirectoryInfo WorkDir { get; init; } = new(Environment.CurrentDirectory);
}
