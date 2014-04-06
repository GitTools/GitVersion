namespace GitVersion
{
    using System;

    public enum OutputType
    {
        BuildServer,

        Json
    }

    public class Arguments
    {
        public Arguments()
        {
            Username = Environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
            Password = Environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
            Output = OutputType.Json;
        }

        public string TargetPath;

        public string TargetUrl;
        public string TargetBranch;

        public string Username;
        public string Password;

        public bool IsHelp;
        public string LogFilePath;
        public string VersionPart;

        public OutputType Output;
        
        public string Proj;
        public string ProjArgs;
        public string Exec;
        public string ExecArgs;
    }
}