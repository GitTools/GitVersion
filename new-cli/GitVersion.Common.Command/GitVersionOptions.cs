using System.IO;

namespace GitVersion.Command
{
    public record GitVersionOptions
    {
        [Option(new[] { "--log-file", "-l" }, "The log file")]
        public FileInfo? LogFile { get; init; } = default;

        [Option("--work-dir", "The working directory with the git repository")]
        public DirectoryInfo WorkDir { get; init; } = new DirectoryInfo(System.Environment.CurrentDirectory);
    }
}