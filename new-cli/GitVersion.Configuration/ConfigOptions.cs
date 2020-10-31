using System.IO;
using GitVersion.Command;

namespace GitVersion.Configuration
{
    [Command("config", "Manages the GitVersion configuration file.")]
    public record ConfigOptions : GitVersionOptions
    {
        [Option("--work-dir", "The working directory with the git repository")]
        public DirectoryInfo WorkDir { get; init; } = default!;
    }
}