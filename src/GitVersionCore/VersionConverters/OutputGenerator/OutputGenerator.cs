using System;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionConverters.OutputGenerator
{
    public class OutputGenerator : IOutputGenerator
    {
        private readonly IConsole console;
        private readonly IOptions<Arguments> options;
        private readonly IBuildServer buildServer;

        public OutputGenerator(IBuildServerResolver buildServerResolver, IConsole console, IOptions<Arguments> options)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            buildServer = buildServerResolver.Resolve();
        }

        public void Execute(VersionVariables variables, Action<string> writter)
        {
            var arguments = options.Value;
            if (arguments.Output.Contains(OutputType.BuildServer))
            {
                buildServer?.WriteIntegration(writter, variables);
            }
            if (arguments.Output.Contains(OutputType.Json))
            {
                switch (arguments.ShowVariable)
                {
                    case null:
                        console.WriteLine(variables.ToString());
                        break;

                    default:
                        if (!variables.TryGetValue(arguments.ShowVariable, out var part))
                        {
                            throw new WarningException($"'{arguments.ShowVariable}' variable does not exist");
                        }

                        console.WriteLine(part);
                        break;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
