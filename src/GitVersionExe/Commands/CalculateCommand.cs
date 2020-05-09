using GitVersion.Extensions;
using GitVersion.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace GitVersion
{

    public class CalculateCommand : Command
    {
        private readonly Logging.IConsole console;
        private readonly IGitVersionTool gitversionTool;
        private readonly GitVersionCommandExecutor executor;

        public CalculateCommand(Logging.IConsole console, IGitVersionTool gitversionTool, GitVersionCommandExecutor executor) : base("calculate", "Calculates version information from your git repository")
        {
            this.console = console;
            this.gitversionTool = gitversionTool;
            this.executor = executor;            
            this.AddOption(new Option<bool>(
            "--normalize",
            "Attempt to mutate your git repository so gitversion has enough information (local branches, commit history etc) to calculate."));
            this.Handler = CommandHandler.Create<GlobalCommandOptions, bool?>(ExecuteAsync);
        }      

        private async Task<int> ExecuteAsync(GlobalCommandOptions globalOptions, bool? normalize)
        {
            // The executor wraps execution of the command logic inside somethng that
            // will do error handling according to the old behaviour.
            return await executor.Execute(globalOptions, async () =>
            {
                if (normalize ?? false)
                {
                    await Normalize();
                }

                var variables = this.gitversionTool.CalculateVersionVariables();
                console.WriteLine(variables.ToString());
                return 0;
            });               
        }

        private Task Normalize()
        {
            throw new NotImplementedException();
        }
    }
}
