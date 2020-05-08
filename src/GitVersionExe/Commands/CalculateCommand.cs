using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace GitVersion
{
    public class CalculateCommand : Command
    {
        private readonly IGitVersionTool gitversionTool;
        private readonly Logging.IConsole console;

        public CalculateCommand(IGitVersionTool gitversionTool, Logging.IConsole console) : base("calculate", "Calculates version information from your git repository")
        {
            this.gitversionTool = gitversionTool;
            this.console = console;
            this.AddOption(new Option<bool>(
            "--normalize",
            "Attempt to mutate your git repository so gitversion has enough information (local branches, commit history etc) to calculate."));
            this.Handler = CommandHandler.Create<bool?>(ExecuteAsync);            
        }

        private async Task ExecuteAsync(bool? normalize)
        {
            if (normalize ?? false)
            {
                await Normalize();
            }

            var variables = this.gitversionTool.CalculateVersionVariables();
            console.WriteLine(variables.ToString());
        }

        private Task Normalize()
        {
            throw new NotImplementedException();
        }
    }
}
