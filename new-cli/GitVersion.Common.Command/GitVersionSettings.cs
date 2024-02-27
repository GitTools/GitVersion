using GitVersion.Infrastructure;

namespace GitVersion;

public record GitVersionSettings
{
    public const string LogFileOptionName = "--log-file";
    public const string VerbosityOption = "--verbosity";

    [Option(LogFileOptionName, "The log file", "-l")]
    public FileInfo? LogFile { get; init; }

    [Option(VerbosityOption, "The verbosity of the logging information")]
    public Verbosity? Verbosity { get; init; } = GitVersion.Infrastructure.Verbosity.Normal;

    [Option("--work-dir", "The working directory with the git repository")]
    public DirectoryInfo? WorkDir { get; init; } = new(Environment.CurrentDirectory);
}
