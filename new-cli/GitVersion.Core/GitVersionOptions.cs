using System.IO;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Core
{
    public class GitVersionOptions
    {
        [Option(new[] { "--log-file", "-l" }, "The log file")]
        public FileInfo LogFile { get; set; }
    }
}