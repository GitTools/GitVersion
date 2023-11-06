using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.App.Tests;

public sealed class ProgramFixture
{
    private readonly IEnvironment environment;
    private List<Action<IServiceCollection>> Overrides { get; } = new();
    private readonly Lazy<string> logger;
    private readonly Lazy<string?> output;

    private readonly string workingDirectory;

    public ProgramFixture(string workingDirectory = "")
    {
        this.workingDirectory = workingDirectory;
        var logBuilder = new StringBuilder();
        var logAppender = new TestLogAppender(m => logBuilder.AppendLine(m));
        ILog log = new Log(logAppender);

        var consoleBuilder = new StringBuilder();
        IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        this.environment = new TestEnvironment();

        WithOverrides(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton(consoleAdapter);
            services.AddSingleton(this.environment);
        });

        this.logger = new Lazy<string>(() => logBuilder.ToString());
        this.output = new Lazy<string?>(() => consoleAdapter.ToString());
    }

    public void WithEnv(params KeyValuePair<string, string>[] envs)
    {
        foreach (var (key, value) in envs)
        {
            this.environment.SetEnvironmentVariable(key, value);
        }
    }

    public void WithOverrides(Action<IServiceCollection> action) => Overrides.Add(action);

    public Task<ProgramFixtureResult> Run(string arg)
    {
        var args = arg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        return Run(args);
    }

    public async Task<ProgramFixtureResult> Run(params string[] args)
    {
        // Create the application and override registrations.
        var program = new Program(builder => Overrides.ForEach(action => action(builder)));

        if (!this.workingDirectory.IsNullOrWhiteSpace())
        {
            args = new[] { "-targetpath", this.workingDirectory }.Concat(args).ToArray();
        }
        await program.RunAsync(args);

        return new ProgramFixtureResult
        {
            ExitCode = SysEnv.ExitCode,
            Output = this.output.Value,
            Log = this.logger.Value
        };
    }
}

public class ProgramFixtureResult
{
    public int ExitCode { get; set; }
    public string? Output { get; set; }
    public string Log { get; set; }

    public GitVersionVariables? OutputVariables
    {
        get
        {
            if (Output.IsNullOrWhiteSpace()) return null;

            var jsonStartIndex = Output.IndexOf("{", StringComparison.Ordinal);
            var jsonEndIndex = Output.IndexOf("}", StringComparison.Ordinal);
            var json = Output.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

            return VersionVariablesHelper.FromJson(json);
        }
    }
}
