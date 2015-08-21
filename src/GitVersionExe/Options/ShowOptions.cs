namespace GitVersion.Options
{
    using System.Collections.Generic;
    using CommandLine;
    using CommandLine.Text;

    [Verb("show",
        HelpText = "Inspect git repository and output deduced version information.")]
    class ShowOptions : LoggingOptions
    {
        [Option('p', "path",
            HelpText = "The path to inspect, defaults to current directory."
            )]
        public string Path { get; set; }

        [Option('o', "output",
            HelpText = "The output format.",
            Default = "Json"
            )]
        public string Output { get; set; }

        [Option('v', "variables")]
        public IEnumerable<string> Variables { get; set; }


        [Usage(ApplicationAlias = "GitVersion")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Inspect the current directory, output version variables in Json format", new ShowOptions());
                yield return new Example("Inspect different directory", new ShowOptions { Path = @"c:\foo\bar\" });
                yield return new Example("Include only some variables", new ShowOptions { Variables = new[] {"SemVer", "Major", "Minor"} });
            }
        }
    }
}