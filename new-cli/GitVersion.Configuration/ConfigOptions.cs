using System.IO;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration
{
    [Command("config", "Manages the GitVersion configuration file.")]
    public class ConfigOptions : GitVersionOptions
    {
        [Option("--work-dir", "The working directory with the git repository")]
        public DirectoryInfo WorkDir { get; set; }
    }
}