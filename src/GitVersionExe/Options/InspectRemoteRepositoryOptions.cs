namespace GitVersion.Options
{
    using System.Collections.Generic;
    using CommandLine;
    using CommandLine.Text;

    [Verb("inspect-remote",
        HelpText = "Inspect a remote repository.")]
    class InspectRemoteRepositoryOptions : LoggingOptions
    {
        [Option('f', "format",
            HelpText = "The output format (Json, KeyValue or Values).",
            Default = "Json"
            )]
        public string Output { get; set; }

        [Option('v', "variables")]
        public IEnumerable<string> Variables { get; set; }

        [Option("url", Required = true,
            HelpText = "Url to remote git repository.")]
        public string Url { get; set; }

        [Option('b', "branch",
            HelpText = "Name of the branch on the remote repository.")]
        string Branch { get; set; }

        [Option('u', "username",
            HelpText = "Username in case authentication is required.")]
        public string UserName { get; set; }

        [Option("password",
            HelpText = "Password in case authentication is required.")]
        public string Password { get; set; }

        [Option('c', "commit",
            HelpText = "The commit id to check. If not specified, the latest available commit on the specified branch will be used.")]
        public string CommitId { get; set; }

        [Option(HelpText = "Target directory to clone to.", Default = "%tmp%")]
        public string DynamicRepositoryLocation { get; set; }

        [Usage(ApplicationAlias = "GitVersion")]
        public static IEnumerable<Example> RemoteExamples
        {
            get
            {
                yield return new Example("Inspect GitVersion's remote repositsory ",
                                         new InspectRemoteRepositoryOptions
                                             {
                                                 Url = "https://github.com/GitTools/GitVersion.git"
                                             });
            }
        }
    }
}