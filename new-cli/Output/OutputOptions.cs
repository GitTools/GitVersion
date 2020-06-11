using System.IO;
using Core;

namespace Output
{
    public class OutputOptions : GlobalOptions
    {
        [Option("--input-file", "The input version file")]
        public FileInfo InputFile { get; set; }
        
        [Option("--output-dir", "The output directory with the git repository")]
        public DirectoryInfo OutputDir { get; set; }
    }
}