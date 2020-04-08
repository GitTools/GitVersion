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
        public GitVersionOptions()
        {
            dotGitDirectory = new Lazy<string>(this.GetDotGitDirectory);
            projectRootDirectory = new Lazy<string>(this.GetProjectRootDirectory);

            AssemblyInfo = new AssemblyInfo();
            Authentication = new AuthenticationInfo();
            ConfigInfo = new ConfigInfo();
            WixInfo = new WixInfo();
            RepositoryInfo = new RepositoryInfo();
        }

        private string workingDirectory;
        public string WorkingDirectory
        {
            get => workingDirectory?.TrimEnd('/', '\\') ?? ".";
            set => workingDirectory = value;
        }

        public string DotGitDirectory => dotGitDirectory.Value;
        public string ProjectRootDirectory => projectRootDirectory.Value;

        public AssemblyInfo AssemblyInfo { get; }
        public AuthenticationInfo Authentication { get; }
        public ConfigInfo ConfigInfo { get; }
        public RepositoryInfo RepositoryInfo { get; }
        public WixInfo WixInfo { get; }

        public bool Init;
        public bool Diag;
        public bool IsVersion;
        public bool IsHelp;

        public bool NoFetch;
        public bool NoCache;
        public bool NoNormalize;
        public bool OnlyTrackedBranches = false;

        public string LogFilePath;
        public string ShowVariable;
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
