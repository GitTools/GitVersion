using System.IO;
using GitVersion.Command;

namespace GitVersion.Calculation
{
    [Command("calculate", "Calculates the version object from the git history.")]
    public record CalculateOptions : GitVersionOptions
    {
        [Option("--work-dir", "The working directory with the git repository")]
        public DirectoryInfo WorkDir { get; init; } = default!;
    }
}