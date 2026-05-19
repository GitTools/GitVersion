using GitVersion.Extensions;

namespace GitVersion;

internal class GitVersionApp(
    IHostApplicationLifetime applicationLifetime,
    IGitVersionExecutor gitVersionExecutor,
    IOptions<GitVersionOptions> options,
    Arguments arguments,
    ITelemetryReporter telemetryReporter)
{
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime.NotNull();
    private readonly IGitVersionExecutor gitVersionExecutor = gitVersionExecutor.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly Arguments arguments = arguments.NotNull();
    private readonly ITelemetryReporter telemetryReporter = telemetryReporter.NotNull();

    public Task RunAsync(CancellationToken _)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            if (gitVersionOptions.IsHelp || gitVersionOptions.IsVersion)
            {
                SysEnv.ExitCode = 0;
            }
            else
            {
                this.telemetryReporter.Report(this.arguments);
                SysEnv.ExitCode = this.gitVersionExecutor.Execute(gitVersionOptions);
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            SysEnv.ExitCode = 1;
        }
        finally
        {
            this.applicationLifetime.StopApplication();
        }

        return Task.CompletedTask;
    }
}
