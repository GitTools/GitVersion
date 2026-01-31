using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion;

internal class GitVersionApp(
    IHostApplicationLifetime applicationLifetime,
    IGitVersionExecutor gitVersionExecutor,
    IOptions<GitVersionOptions> options)
{
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime.NotNull();
    private readonly IGitVersionExecutor gitVersionExecutor = gitVersionExecutor.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public Task RunAsync(CancellationToken _)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            LoggingEnricher.Configure(gitVersionOptions);
            SysEnv.ExitCode = this.gitVersionExecutor.Execute(gitVersionOptions);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            SysEnv.ExitCode = 1;
        }

        this.applicationLifetime.StopApplication();
        return Task.CompletedTask;
    }
}
