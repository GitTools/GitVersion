using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Logging;
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
        var consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        this.environment = new TestEnvironment();

        WithOverrides(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton<IConsole>(consoleAdapter);
            services.AddSingleton(this.environment);
        });

        this.logger = new(() => logBuilder.ToString());
        this.output = new(() => consoleAdapter.ToString());
    }

    public void WithEnv(params KeyValuePair<string, string>[] envs)
    {
        foreach (var (key, value) in envs)
        {
            this.environment.SetEnvironmentVariable(key, value);
        }
    }

    public void WithOverrides(Action<IServiceCollection> action) => Overrides.Add(action);

    public Task<ExecutionResults> Run(string arg)
    {
        var args = arg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        return Run(args);
    }

    public async Task<ExecutionResults> Run(params string[] args)
    {
        // Create the application and override registrations.
        var program = new Program(builder => Overrides.ForEach(action => action(builder)));

        if (!this.workingDirectory.IsNullOrWhiteSpace())
        {
            args = ["-targetpath", this.workingDirectory, .. args];
        }
        await program.RunAsync(args);

        return new(SysEnv.ExitCode, this.output.Value, this.logger.Value);
    }
}
