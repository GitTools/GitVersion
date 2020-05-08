using Microsoft.Extensions.Options;
using System.CommandLine;

namespace GitVersion
{
    public class GitVersionRootCommand : RootCommand
    {
        public GitVersionRootCommand(CalculateCommand calculateCommand, IOptions<GitVersionOptions> globalOptions) : base("Versioning for your git repository, solved!")
        {
            // this.AddGlobalOption()
            //this.AddGlobalOption(new Option("--target-path") { Argument = new Argument<LoggingMethod>() });
            //this.AddGlobalOption(new Option("--logging-method") { Argument = new Argument<LoggingMethod>() });
            GlobalOptions = globalOptions.Value;
            this.AddCommand(calculateCommand);
        }

        public GitVersionOptions GlobalOptions { get; }
    }
}
