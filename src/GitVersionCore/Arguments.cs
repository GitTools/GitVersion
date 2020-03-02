using System;
using System.Collections.Generic;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.OutputFormatters;

namespace GitVersion
{
    public class Arguments
    {
        public AuthenticationInfo Authentication;

        public Config OverrideConfig;
        public bool HasOverrideConfig;

        public string TargetPath;
        public string ConfigFile;

        public string TargetUrl;
        public string TargetBranch;
        public string CommitId;
        public string DynamicRepositoryLocation;

        public bool Init;
        public bool Diag;
        public bool IsVersion;
        public bool IsHelp;
        public string LogFilePath;
        public string ShowVariable;

        [Obsolete]
        public string Proj;
        [Obsolete]
        public string ProjArgs;
        [Obsolete]
        public string Exec;
        [Obsolete]
        public string ExecArgs;

        public bool UpdateWixVersionFile;

        public bool ShowConfig;
        public bool NoFetch;
        public bool NoCache;
        public bool NoNormalize;

        public ISet<OutputType> Output = new HashSet<OutputType>();
        public Verbosity Verbosity = Verbosity.Normal;

        public bool UpdateAssemblyInfo;
        public ISet<string> UpdateAssemblyInfoFileName = new HashSet<string>();
        public bool EnsureAssemblyInfo;
        public void AddAssemblyInfoFileName(string fileName)
        {
            UpdateAssemblyInfoFileName.Add(fileName);
        }
    }
}
