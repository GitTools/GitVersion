using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;
using GitVersion.Telemetry;

namespace GitVersion;

/// <summary>Dispatches CLI operations and runs eligible telemetry before version calculation without changing its exit code.</summary>
[method: SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
    Justification = "The application entry point explicitly receives its independent DI services.")]
internal class GitVersionApp(
    IHostApplicationLifetime applicationLifetime,
    Lazy<IGitVersionExecutor> gitVersionExecutor,
    IConfigurationMigrationExecutor configurationMigrationExecutor,
    ConfigurationValidationExecutor configurationValidationExecutor,
    FeatureSelections selections,
    ILogger<GitVersionApp> logger,
    ITelemetryReporter telemetry,
    IOptions<GitVersionOptions> options)
{
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime.NotNull();
    private readonly Lazy<IGitVersionExecutor> gitVersionExecutor = gitVersionExecutor.NotNull();
    private readonly IConfigurationMigrationExecutor configurationMigrationExecutor = configurationMigrationExecutor.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    /// <summary>Executes the requested operation, reports failures on stderr, and always stops the application host.</summary>
    /// <param name="cancellationToken">Cancels any eligible telemetry request before version calculation.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            var gitVersionOptions = this.options.Value;
            if (gitVersionOptions.IsCliSchema)
            {
                SysEnv.ExitCode = 0;
                return;
            }
            if (gitVersionOptions.ConfigurationInfo.Validate)
            {
                SysEnv.ExitCode = configurationValidationExecutor.Execute(gitVersionOptions);
                return;
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
                await telemetry.ReportAsync(cancellationToken).ConfigureAwait(false);
                SysEnv.ExitCode = this.gitVersionExecutor.Value.Execute(gitVersionOptions);
            }
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            SysEnv.ExitCode = 1;
        }
        finally
        {
            this.applicationLifetime.StopApplication();
        }
    }
}
