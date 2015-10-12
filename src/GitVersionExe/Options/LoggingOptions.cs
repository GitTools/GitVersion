namespace GitVersion.Options
{
    using CommandLine;

    class LoggingOptions
    {
        [Option(
            HelpText = "Prints all system messages to standard output.")]
        public bool Verbose { get; set; }

        [Option(
            HelpText = "Specify log file for system messages.")]
        public string Log { get; set; }
    }
}