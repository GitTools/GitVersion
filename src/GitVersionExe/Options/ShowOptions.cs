namespace GitVersion.Options
{
    using CommandLine;

    [Verb("show",
        HelpText = "Inspect git repository and output deduced version information.")]
    class ShowOptions : LoggingOptions
    {
        [Option('p', "path",
            HelpText = "The path to inspect, defaults to current directory."
            )]
        public string Path { get; set; }

        // variables to include

        // output type (plain, json, ?java properties, ?xml)
    }
}