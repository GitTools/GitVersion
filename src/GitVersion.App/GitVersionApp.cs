using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitVersionApp(ILog log, IHostApplicationLifetime applicationLifetime, IGitVersionExecutor gitVersionExecutor, IOptions<GitVersionOptions> options)
    : IHostedService
{
    private readonly ILog log = log.NotNull();
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime.NotNull();
    private readonly IGitVersionExecutor gitVersionExecutor = gitVersionExecutor.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            this.log.Verbosity = gitVersionOptions.Verbosity;
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
