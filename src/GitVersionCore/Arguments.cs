using System.Collections.Generic;
using GitVersion.Configuration;
using GitVersion.Log;
using GitVersion.OutputFormatters;

namespace GitVersion
{
    public class Arguments
    {
        public Arguments()
        {
            Authentication = new Authentication();
            OverrideConfig = new Config();
            Output = OutputType.Json;
            UpdateAssemblyInfoFileName = new HashSet<string>();
            Verbosity = Verbosity.Normal;
        }

        public Authentication Authentication;

        public Config OverrideConfig;
        public bool HasOverrideConfig { get; set; }
        public IConfigFileLocator ConfigFileLocator { get; set; }

        public string TargetPath;

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

        public OutputType Output;

        public string Proj;
        public string ProjArgs;
        public string Exec;
        public string ExecArgs;

        public bool UpdateAssemblyInfo;
        public ISet<string> UpdateAssemblyInfoFileName;
        public bool EnsureAssemblyInfo;

        public bool UpdateWixVersionFile;

        public bool ShowConfig;
        public bool NoFetch;
        public bool NoCache;
        public bool NoNormalize;

        public Verbosity Verbosity;

        public void AddAssemblyInfoFileName(string fileName)
        {
            UpdateAssemblyInfoFileName.Add(fileName);
        }
    }
}
