using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitVersion;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GitVersionExe.Tests
{
    public sealed class ProgramFixture
    {
        private IEnvironment environment;
        public List<Action<IServiceCollection>> Overrides { get; } = new List<Action<IServiceCollection>>();
        private readonly Lazy<string> logger;
        private readonly Lazy<string> output;

        private readonly string workingDirectory;

        public ProgramFixture(string workingDirectory = "")
        {
            this.workingDirectory = workingDirectory;
            var logBuilder = new StringBuilder();
            var logAppender = new TestLogAppender(m => logBuilder.AppendLine(m));
            ILog log = new Log(logAppender);

            var consoleBuilder = new StringBuilder();
            IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

            environment = new TestEnvironment();

            Overrides.Add(services =>
            {
                services.AddSingleton(log);
                services.AddSingleton(consoleAdapter);
                services.AddSingleton(environment);
            });

            logger = new Lazy<string>(() => logBuilder.ToString());
            output = new Lazy<string>(() => consoleAdapter.ToString());
        }

        public void WithEnv(params KeyValuePair<string, string>[] envs)
        {
            foreach (var env in envs)
            {
                environment.SetEnvironmentVariable(env.Key, env.Value);
            }
        }

        public Task<ProgramFixtureResult> Run(string arg)
        {
            var args = arg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            return Run(args);
        }

        public async Task<ProgramFixtureResult> Run(params string[] args)
        {
            // Create the application and override registrations.
            var program = new Program(builder => Overrides.ForEach(action => action(builder)));

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                args = new[] { "-targetpath", workingDirectory }.Concat(args).ToArray();
            }
            await program.RunAsync(args);

            return new ProgramFixtureResult
            {
                ExitCode = System.Environment.ExitCode,
                Output = output.Value,
                Log = logger.Value
            };
        }
    }

    public class ProgramFixtureResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Log { get; set; }

        public VersionVariables OutputVariables
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Output)) return null;

                var jsonStartIndex = Output.IndexOf("{", StringComparison.Ordinal);
                var jsonEndIndex = Output.IndexOf("}", StringComparison.Ordinal);
                var json = Output.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

                var outputVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return VersionVariables.FromDictionary(outputVariables);
            }
        }

    }
}
