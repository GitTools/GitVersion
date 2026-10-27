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

    public Task<int> RunAsync(CancellationToken _)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            if (gitVersionOptions.IsCliSchema)
            {
                return Task.FromResult(0);
            }
            if (gitVersionOptions.ConfigurationInfo.Validate)
            {
                return Task.FromResult(configurationValidationExecutor.Execute(gitVersionOptions));
            }
            selections.Log(logger);
            if (gitVersionOptions.IsHelp || gitVersionOptions.IsVersion)
            {
                return Task.FromResult(0);
            }
            else if (gitVersionOptions.ConfigurationMigrationInfo.IsMigration)
            {
                return Task.FromResult(this.configurationMigrationExecutor.Execute(gitVersionOptions));
            }
            else
            {
                return Task.FromResult(this.gitVersionExecutor.Value.Execute(gitVersionOptions));
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return Task.FromResult(1);
        }
        finally
        {
            this.applicationLifetime.StopApplication();
        }
    }
}
