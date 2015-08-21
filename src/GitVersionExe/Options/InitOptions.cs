namespace GitVersion.Options
{
    using CommandLine;

    [Verb("init",
        HelpText = "Start configuration utility for gitversion.")]
    class InitOptions : LoggingOptions
    {
        [Option('p', "path",
            HelpText = "The path to inspect, defaults to current directory."
            )]
        public string Path { get; set; }
    }
}