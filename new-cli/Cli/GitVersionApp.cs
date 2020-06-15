using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Core;

namespace Cli
{
    internal class GitVersionApp : RootCommand
    {
        public GitVersionApp(IEnumerable<ICommandHandler> commandHandlers)
        {
            foreach (var commandHandler in commandHandlers)
            {
                var command = commandHandler.GetCommand();
                AddCommand(command);
            }
        }

        public Task<int> RunAsync(string[] args)
        {
            return new CommandLineBuilder(this)
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }
    }
}