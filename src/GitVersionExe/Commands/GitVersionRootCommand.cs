using System.CommandLine;

namespace GitVersion
{
    public class GitVersionRootCommand : RootCommand
    {
        public GitVersionRootCommand(CalculateCommand calculateCommand) : base("Versioning for your git repository, solved!")
        {
            var loggingMethodOptions = new Option("--logging-method") { Argument = new Argument<LoggingMethod>() };
            this.AddGlobalOption(loggingMethodOptions);

            var logFileOption = new Option("--logfilepath") { Argument = new Argument<string> { } };
            logFileOption.Required = false;
            this.AddGlobalOption(logFileOption);

            this.AddCommand(calculateCommand);
        }      
    }
}
