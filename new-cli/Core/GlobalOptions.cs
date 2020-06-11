using System.IO;

namespace Core
{
    public class GlobalOptions
    {
        [Option(new[] { "--log-file", "-l" }, "The log file")]
        public FileInfo LogFile { get; set; }
    }
}