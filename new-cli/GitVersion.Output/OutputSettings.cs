namespace GitVersion.Commands;

public record OutputSettings : GitVersionSettings
{
    public Lazy<string> VersionInfo { get; } = new(() => Console.IsInputRedirected ? Console.ReadLine() ?? string.Empty : string.Empty);

    [Option("--input-file", "The input version file")]
    public required FileInfo InputFile { get; init; }

    [Option("--output-dir", "The output directory with the git repository")]
    public required DirectoryInfo OutputDir { get; init; }
}
