using System.IO;
using Core;

namespace Calculate
{
    public class CalculateOptions : GlobalOptions
    {
        [Option("--work-dir", "The working directory with the git repository")]
        public DirectoryInfo WorkDir { get; set; }
    }
}