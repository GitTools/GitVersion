using System.IO;

namespace GitVersion.Command
{
    public record GitVersionOptions
    {
        [Option(new[] { "--log-file", "-l" }, "The log file")]
        public FileInfo LogFile { get; init; } = default!; // see https://docs.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#non-nullable-properties-and-initialization
    }
}