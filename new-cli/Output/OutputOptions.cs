using System.IO;
using Core;

namespace Output
{
    [Command("output", "Outputs the version object.")]
    public class OutputOptions : GitVersionOptions
    {
        [Option("--input-file", "The input version file")]
        public FileInfo InputFile { get; set; }
        
        [Option("--output-dir", "The output directory with the git repository")]
        public DirectoryInfo OutputDir { get; set; }
    }
}