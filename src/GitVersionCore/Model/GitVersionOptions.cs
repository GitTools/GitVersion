using System;
using System.Collections.Generic;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model;

namespace GitVersion
{
    public class GitVersionOptions
    {
        private Lazy<string> dotGitDirectory;
        private Lazy<string> gitRepositoryWorkingDirectory;

        public GitVersionOptions()
        {
            WorkingDirectory = System.Environment.CurrentDirectory;
            dotGitDirectory = new Lazy<string>(this.GetDotGitDirectory);
            gitRepositoryWorkingDirectory = new Lazy<string>(this.GetRepositoryWorkingDirectory);
        }

        public string[] Args { get; set; }
        public string WorkingDirectory { get; set; }
        public string DotGitDirectory => dotGitDirectory.Value;
        public string GitRepositoryWorkingDirectory => gitRepositoryWorkingDirectory.Value;
        public bool LogToConsole { get; set; } = false;
        public string LogFilePath;

        //public AssemblyInfoData AssemblyInfo { get; } = new AssemblyInfoData();
        public AuthenticationInfo Authentication { get; } = new AuthenticationInfo();
        public ConfigInfo ConfigInfo { get; } = new ConfigInfo();
        public RepositoryInfo RepositoryInfo { get; } = new RepositoryInfo();
        public WixInfo WixInfo { get; } = new WixInfo();
        public Settings Settings { get; } = new Settings();

        public bool Init;
        public bool Diag;
        public bool IsVersion;
        public bool IsHelp;

        public string ShowVariable;
        public string OutputFile;
        public ISet<OutputType> Output = new HashSet<OutputType>();
        public Verbosity Verbosity = Verbosity.Normal;

    }
}
