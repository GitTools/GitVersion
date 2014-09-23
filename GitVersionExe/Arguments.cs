namespace GitVersion
{
    public class Arguments
    {
        public Arguments()
        {
            Authentication = new Authentication();
            Output = OutputType.Json;
        }

        public Authentication Authentication;

        public string TargetPath;

        public string TargetUrl;
        public string TargetBranch;


        public bool IsHelp;
        public string LogFilePath;
        public string VersionPart;

        public OutputType Output;
        
        public string Proj;
        public string ProjArgs;
        public string Exec;
        public string ExecArgs;

        public bool UpdateAssemblyInfo;
        public string UpdateAssemblyInfoFileName;
        public string PreReleaseTag;
    }
}