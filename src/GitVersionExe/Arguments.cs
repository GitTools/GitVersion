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

        public bool ShowConfig;
        public bool NoFetch;

        public void AddAssemblyInfoFileName(string fileName)
        {
            UpdateAssemblyInfoFileName.Add(fileName);
        }
    }
}