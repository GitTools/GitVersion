using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion;

internal class GitVersionApp(
    ILog log,
    IHostApplicationLifetime applicationLifetime,
    IGitVersionExecutor gitVersionExecutor,
    IServiceProvider serviceProvider,
    IOptions<GitVersionOptions> options)
{
    private readonly ILog log = log.NotNull();
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime.NotNull();
    private readonly IGitVersionExecutor gitVersionExecutor = gitVersionExecutor.NotNull();
    private readonly IServiceProvider serviceProvider = serviceProvider.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public Task RunAsync(CancellationToken _)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            this.log.Verbosity = gitVersionOptions.Verbosity;

            SysEnv.ExitCode = IsHelpOrVersionCommand(gitVersionOptions, out var exitCode)
                ? exitCode
                : this.gitVersionExecutor.Execute(gitVersionOptions);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            SysEnv.ExitCode = 1;
        }

        this.applicationLifetime.StopApplication();
        return Task.CompletedTask;
    }

    private bool IsHelpOrVersionCommand(GitVersionOptions gitVersionOptions, out int exitCode)
    {
        if (gitVersionOptions.IsVersion)
        {
            var versionWriter = serviceProvider.GetRequiredService<IVersionWriter>();
            var assembly = Assembly.GetExecutingAssembly();
            versionWriter.Write(assembly);
            exitCode = 0;
            return true;
        }

        if (gitVersionOptions.IsHelp)
        {
            var helpWriter = serviceProvider.GetRequiredService<IHelpWriter>();
            helpWriter.Write();
            exitCode = 0;
            return true;
        }

        exitCode = 0;
        return false;
    }
}
