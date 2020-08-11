using System.IO;

namespace GitVersion.Command
{
    public class GitVersionOptions
    {
        [Option(new[] { "--log-file", "-l" }, "The log file")]
        public FileInfo LogFile { get; set; }
    }
}