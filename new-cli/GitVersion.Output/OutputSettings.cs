using System;
using System.IO;
using GitVersion.Command;

namespace GitVersion.Output;

[Command("output", "Outputs the version object.")]
public record OutputSettings : GitVersionSettings
{
    public Lazy<string> VersionInfo { get; } = new(() => Console.IsInputRedirected ? Console.ReadLine() : "");

    [Option("--input-file", "The input version file")]
    public FileInfo InputFile { get; init; } = default!;

    [Option("--output-dir", "The output directory with the git repository")]
    public DirectoryInfo OutputDir { get; init; } = default!;
}