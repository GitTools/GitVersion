using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace Cli
{
    internal class GitVersionApp : RootCommand
    {
        public GitVersionApp(IEnumerable<Command> commands)
        {
            AddGlobalOption(new Option<FileInfo>(new[] { "--log-file", "-l" }, "The log file"));

            foreach (var command in commands)
            {
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