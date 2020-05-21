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
        private Lazy<string> projectRootDirectory;
        private Lazy<string> dynamicGitRepositoryPath;
        private Lazy<string> gitRootPath;

        public GitVersionOptions()
        {
            dotGitDirectory = new Lazy<string>(this.GetDotGitDirectory);
            projectRootDirectory = new Lazy<string>(this.GetProjectRootDirectory);
            dynamicGitRepositoryPath = new Lazy<string>(this.GetDynamicGitRepositoryPath);
            gitRootPath = new Lazy<string>(this.GetGitRootPath);
        }

        public string WorkingDirectory { get; set; }

        public string DotGitDirectory => dotGitDirectory.Value;
        public string ProjectRootDirectory => projectRootDirectory.Value;
        public string DynamicGitRepositoryPath => dynamicGitRepositoryPath.Value;
        public string GitRootPath => gitRootPath.Value;

        public AssemblyInfoData AssemblyInfo { get; } = new AssemblyInfoData();
        public AuthenticationInfo Authentication { get; } = new AuthenticationInfo();
        public ConfigInfo ConfigInfo { get; } = new ConfigInfo();
        public RepositoryInfo RepositoryInfo { get; } = new RepositoryInfo();
        public WixInfo WixInfo { get; } = new WixInfo();
        public Settings Settings { get; } = new Settings();

        public bool Init;
        public bool Diag;
        public bool IsVersion;
        public bool IsHelp;

        public string LogFilePath;
        public string ShowVariable;
        public string OutputFile;
        public ISet<OutputType> Output = new HashSet<OutputType>();
        public Verbosity Verbosity = Verbosity.Normal;

        [Obsolete]
        public string Proj;
        [Obsolete]
        public string ProjArgs;
        [Obsolete]
        public string Exec;
        [Obsolete]
        public string ExecArgs;
    }
}
