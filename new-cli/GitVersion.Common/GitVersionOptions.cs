using System.IO;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion
{
    public class GitVersionOptions
    {
        [Option(new[] { "--log-file", "-l" }, "The log file")]
        public FileInfo LogFile { get; set; }
    }
}