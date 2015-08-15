namespace GitVersion
{
    using System;

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
        public string CommitId;
        public string DynamicRepositoryLocation;

        public bool Init;

        public bool IsHelp;
        public string LogFilePath;
        public string ShowVariable;

        public OutputType Output;

        public void SetOutPutType(string value)
        {
            if (!Enum.TryParse(value, true, out Output))
            {
                throw new WarningException(string.Format("Value '{0}' cannot be parsed as output type, please use 'json' or 'buildserver'", value));
            }
        }

        public string Proj;
        public string ProjArgs;
        public string Exec;
        public string ExecArgs;

        public bool UpdateAssemblyInfo;
        public string UpdateAssemblyInfoFileName;

        public bool ShowConfig;
        public bool NoFetch { get; set; }

    }
}