using GitVersion.Command;

namespace GitVersion.Output;

public class OutputSettings : GitVersionSettings
{
    public Lazy<string> VersionInfo { get; } = new(() => (Console.IsInputRedirected ? Console.ReadLine() : string.Empty)!);

    [Option("--input-file", "The input version file")]
    public FileInfo InputFile { get; init; } = default!;

    [Option("--output-dir", "The output directory with the git repository")]
    public DirectoryInfo OutputDir { get; init; } = default!;
}