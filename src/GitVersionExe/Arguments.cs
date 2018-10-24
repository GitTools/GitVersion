namespace GitVersion
{
    using System.Collections.Generic;

    public class Arguments
    {
        public Arguments()
        {
            Authentication = new Authentication();
            OverrideConfig = new Config();
            Output = OutputType.Json;
            UpdateAssemblyInfoFileName = new HashSet<string>();
            Verbosity = VerbosityLevel.Info;
        }

        public Authentication Authentication;

        public Config OverrideConfig;
        public bool HasOverrideConfig { get; set; }

        public string TargetPath;

        public string TargetUrl;
        public string TargetBranch;
        public string CommitId;
        public string DynamicRepositoryLocation;

        public bool Init;
#if NETDESKTOP
        public bool Diag;
#endif
        public bool IsVersion;
        public bool IsHelp;
        public string LogFilePath;
        public string ShowVariable;

        public OutputType Output;
#if NETDESKTOP
        public string Proj;
        public string ProjArgs;
        public string Exec;
        public string ExecArgs;
#endif
        public bool UpdateAssemblyInfo;
        public ISet<string> UpdateAssemblyInfoFileName;
        public bool EnsureAssemblyInfo;

        public bool ShowConfig;
        public bool NoFetch;
        public bool NoCache;

        public VerbosityLevel Verbosity;

        public void AddAssemblyInfoFileName(string fileName)
        {
            UpdateAssemblyInfoFileName.Add(fileName);
        }
    }
}