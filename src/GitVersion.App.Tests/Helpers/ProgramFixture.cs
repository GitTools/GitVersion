using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;

namespace GitVersion.App.Tests;

public sealed class ProgramFixture
{
    private readonly TestEnvironment environment;
    private List<Action<IServiceCollection>> Overrides { get; } = [];
    private readonly Lazy<string> logger;
    private readonly Lazy<string?> output;

    private readonly string workingDirectory;

    public ProgramFixture(string workingDirectory = "")
    {
        this.workingDirectory = workingDirectory;
        var logBuilder = new StringBuilder();
        var testLoggerFactory = new TestLoggerFactory(m => logBuilder.AppendLine(m));

        var consoleBuilder = new StringBuilder();
        var consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        this.environment = new TestEnvironment();

        WithOverrides(services =>
        {
            services.AddSingleton<ILoggerFactory>(testLoggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(TestLogger<>));
            services.AddSingleton<IConsole>(consoleAdapter);
            services.AddSingleton<IEnvironment>(this.environment);
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
        var args = arg.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        return Run(args);
    }

    public async Task<ExecutionResults> Run(params string[] args)
    {
        if (!this.workingDirectory.IsNullOrWhiteSpace())
        {
            args = ["-targetpath", this.workingDirectory, .. args];
        }

        var builder = CliHost.CreateCliHostBuilder(args);

        Overrides.ForEach(action => action(builder.Services));

        var host = builder.Build();
        var app = host.Services.GetRequiredService<GitVersionApp>();
        await app.RunAsync(CancellationToken.None);

        return new(SysEnv.ExitCode, this.output.Value, this.logger.Value);
    }
}
