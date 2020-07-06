using System.IO;
using GitVersion.Core;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Config
{
    [Command("config", "Manages the GitVersion configuration file.")]
    public class ConfigOptions : GitVersionOptions
    {
        [Option("--work-dir", "The working directory with the git repository")]
        public DirectoryInfo WorkDir { get; set; }
    }
}