using System.IO;
using GitVersion.Command;

namespace GitVersion.Normalization
{
    [Command("normalize", "Normalizes the git repository for GitVersion calculations.")]
    public record NormalizeOptions : GitVersionOptions
    {
        [Option("--work-dir", "The working directory with the git repository")]
        public DirectoryInfo WorkDir { get; init; } = default!;
    }
}