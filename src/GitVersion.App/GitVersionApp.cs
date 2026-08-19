using GitVersion.Extensions;

namespace GitVersion;

internal class GitVersionApp(
    IHostApplicationLifetime applicationLifetime,
    IGitVersionExecutor gitVersionExecutor,
    IConfigurationMigrationExecutor configurationMigrationExecutor,
    IOptions<GitVersionOptions> options)
{
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime.NotNull();
    private readonly IGitVersionExecutor gitVersionExecutor = gitVersionExecutor.NotNull();
    private readonly IConfigurationMigrationExecutor configurationMigrationExecutor = configurationMigrationExecutor.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public Task RunAsync(CancellationToken _)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            if (gitVersionOptions.IsHelp || gitVersionOptions.IsVersion)
            {
                SysEnv.ExitCode = 0;
            }
            else if (gitVersionOptions.ConfigurationMigrationInfo.IsMigration)
            {
                SysEnv.ExitCode = this.configurationMigrationExecutor.Execute(gitVersionOptions);
            }
            else
            {
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
