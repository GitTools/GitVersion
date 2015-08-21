namespace GitVersion.Options
{
    using CommandLine;

    [Verb("inspect-remote", 
        HelpText = "Inspect a remote repository.")]
    class InspectRemoteRepositoryOptions : LoggingOptions
    {
        [Option("url", Required = true,
            HelpText = "Url to remote git repository.")]
        string Url { get; set; }

        [Option('b',"branch", 
            HelpText = "Name of the branch on the remote repository.")]
        string Branch { get; set; }

        [Option('u', "username",
            HelpText = "Username in case authentication is required.")]
        public string UserName { get; set; }

        [Option('p', "password",
            HelpText = "Password in case authentication is required.")]
        public string Password { get; set; }

        [Option('c', "commit",
            HelpText = "The commit id to check. If not specified, the latest available commit on the specified branch will be used.")]
        public string CommitId { get; set; }

        [Option(HelpText = "Target directory to clone to.", Default = "%tmp%")]
        public string DynamicRepositoryLocation { get; set; }
    }
}