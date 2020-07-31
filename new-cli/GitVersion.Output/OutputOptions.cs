using System;
using System.IO;
using GitVersion.Infrastructure;

namespace GitVersion.Output
{
    [Command("output", "Outputs the version object.")]
    public class OutputOptions : GitVersionOptions
    {
        public Lazy<string> VersionInfo { get; set; } = new Lazy<string>(() => Console.IsInputRedirected ? Console.ReadLine() : null);

        [Option("--input-file", "The input version file")]
        public FileInfo InputFile { get; set; }
        
        [Option("--output-dir", "The output directory with the git repository")]
        public DirectoryInfo OutputDir { get; set; }
    }
}