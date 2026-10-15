using GitVersion.Extensions;

namespace GitVersion;

internal class GitVersionApp(
    IHostApplicationLifetime applicationLifetime,
    Lazy<IGitVersionExecutor> gitVersionExecutor,
    IConfigurationMigrationExecutor configurationMigrationExecutor,
    ConfigurationValidationExecutor configurationValidationExecutor,
    FeatureSelections selections,
    ILogger<GitVersionApp> logger,
    IOptions<GitVersionOptions> options)
{
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime.NotNull();
    private readonly Lazy<IGitVersionExecutor> gitVersionExecutor = gitVersionExecutor.NotNull();
    private readonly IConfigurationMigrationExecutor configurationMigrationExecutor = configurationMigrationExecutor.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public Task RunAsync(CancellationToken _)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            if (gitVersionOptions.IsCliSchema)
            {
                SysEnv.ExitCode = 0;
                return Task.CompletedTask;
            }
            if (gitVersionOptions.ConfigurationInfo.Validate)
            {
                SysEnv.ExitCode = configurationValidationExecutor.Execute(gitVersionOptions);
                return Task.CompletedTask;
            }
            selections.Log(logger);
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
                SysEnv.ExitCode = this.gitVersionExecutor.Value.Execute(gitVersionOptions);
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
