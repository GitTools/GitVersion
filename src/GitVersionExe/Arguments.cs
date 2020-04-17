using System;
using System.Collections.Generic;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersion.Model.Configuration;

namespace GitVersion
{
    public class Arguments
    {
        public AuthenticationInfo Authentication = new AuthenticationInfo();

        public string ConfigFile;
        public Config OverrideConfig;
        public bool ShowConfig;

        public string TargetPath;

        public bool UpdateWixVersionFile;

        public string TargetUrl;
        public string TargetBranch;
        public string CommitId;
        public string DynamicRepositoryClonePath;

        public bool Init;
        public bool Diag;
        public bool IsVersion;
        public bool IsHelp;

        public bool NoFetch;
        public bool NoCache;
        public bool NoNormalize;

        public string LogFilePath;
        public string ShowVariable;
        public ISet<OutputType> Output = new HashSet<OutputType>();
        public Verbosity Verbosity = Verbosity.Normal;

        public bool UpdateAssemblyInfo;
        public ISet<string> UpdateAssemblyInfoFileName = new HashSet<string>();
        public bool EnsureAssemblyInfo;

        [Obsolete]
        public string Proj;
        [Obsolete]
        public string ProjArgs;
        [Obsolete]
        public string Exec;
        [Obsolete]
        public string ExecArgs;

        public GitVersionOptions ToOptions()
        {
            return new GitVersionOptions
            {
                WorkingDirectory = TargetPath.TrimEnd('/', '\\'),

                AssemblyInfo =
                {
                    ShouldUpdate = UpdateAssemblyInfo,
                    EnsureAssemblyInfo = EnsureAssemblyInfo,
                    Files = UpdateAssemblyInfoFileName,
                },

                Authentication =
                {
                    Username = Authentication.Username,
                    Password = Authentication.Password,
                    Token = Authentication.Token,
                },

                ConfigInfo =
                {
                    ConfigFile = ConfigFile,
                    OverrideConfig = OverrideConfig,
                    ShowConfig = ShowConfig,
                },

                RepositoryInfo =
                {
                    TargetUrl = TargetUrl,
                    TargetBranch = TargetBranch,
                    CommitId = CommitId,
                    DynamicRepositoryClonePath = DynamicRepositoryClonePath,
                },

                Settings =
                {
                    NoFetch = NoFetch,
                    NoCache = NoCache,
                    NoNormalize = NoNormalize,
                },

                WixInfo =
                {
                    ShouldUpdate = UpdateWixVersionFile,
                },

                Init = Init,
                Diag = Diag,
                IsVersion = IsVersion,
                IsHelp = IsHelp,

                LogFilePath = LogFilePath,
                ShowVariable = ShowVariable,
                Verbosity = Verbosity,
                Output = Output,

                // TODO obsolete to be removed in version 6.0.0
                Proj = Proj,
                ProjArgs = ProjArgs,
                Exec = Exec,
                ExecArgs = ExecArgs,
            };
        }
    }
}
