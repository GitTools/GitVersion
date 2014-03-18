namespace GitVersion
{
    using System;

    public class Arguments
    {
        public Arguments()
        {
            Username = Environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
            Password = Environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
        }

        public string TargetPath;

        public string TargetUrl;
        public string TargetBranch;

        public string Username;
        public string Password;

        public bool IsHelp;
        public string LogFilePath;
        public string VersionPart;
    }
}