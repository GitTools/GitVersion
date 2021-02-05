using System;
using System.IO;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionConverters.OutputGenerator
{
    public interface IOutputGenerator : IVersionConverter<OutputContext>
    {
    }

    public class OutputGenerator : IOutputGenerator
    {
        private readonly IConsole console;
        private readonly ILog log;
        private readonly IFileSystem fileSystem;
        private readonly IOptions<GitVersionOptions> options;
        private readonly ICurrentBuildAgent buildAgent;

        public OutputGenerator(ICurrentBuildAgent buildAgent, IConsole console, ILog log, IFileSystem fileSystem, IOptions<GitVersionOptions> options)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.buildAgent = buildAgent;
        }

        public void Execute(VersionVariables variables, OutputContext context)
        {
            var gitVersionOptions = options.Value;
            if (gitVersionOptions.Output.Contains(OutputType.BuildServer))
            {
                buildAgent?.WriteIntegration(console.WriteLine, variables, context.UpdateBuildNumber ?? true);
            }
            if (gitVersionOptions.Output.Contains(OutputType.File))
            {
                var retryOperation = new OperationWithExponentialBackoff<IOException>(new ThreadSleep(), log, () => fileSystem.WriteAllText(context.OutputFile, variables.ToString()), maxRetries: 6);
                retryOperation.ExecuteAsync().Wait();
            }
            if (gitVersionOptions.Output.Contains(OutputType.Json))
            {
                switch (gitVersionOptions.ShowVariable)
                {
                    case null:
                        console.WriteLine(variables.ToString());
                        break;

                    default:
                        if (!variables.TryGetValue(gitVersionOptions.ShowVariable, out var part))
                        {
                            throw new WarningException($"'{gitVersionOptions.ShowVariable}' variable does not exist");
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
