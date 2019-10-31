using System.Collections.Generic;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.OutputFormatters;

namespace GitVersion
{
    public class Arguments
    {
        public Authentication Authentication;

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

        public string Proj;
        public string ProjArgs;
        public string Exec;
        public string ExecArgs;

        public bool UpdateWixVersionFile;

        public bool ShowConfig;
        public bool NoFetch;
        public bool NoCache;
        public bool NoNormalize;

        public OutputType Output = OutputType.Json;
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
