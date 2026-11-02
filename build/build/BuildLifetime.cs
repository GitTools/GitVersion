using System.Globalization;
using Build.Utilities;
using Common.Lifetime;
using Common.Utilities;
using EnvVars = Build.Utilities.EnvVars;

namespace Build;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    /// <summary>Initializes build settings, including the release telemetry date shared by all artifacts.</summary>
    public override void Setup(BuildContext context, ISetupContext info)
    {
        base.Setup(context, info);

        context.MsBuildConfiguration = context.Argument(Arguments.Configuration, Constants.DefaultConfiguration);
        context.EnabledUnitTests = context.IsEnabled(EnvVars.EnabledUnitTests);
        SetTelemetryMetadata(context);

        context.Credentials = Credentials.GetCredentials(context);

        if (context.Version is not null)
        {
            SetMsBuildSettingsVersion(context);
        }

        context.StartGroup("Build Setup");
        LogBuildInformation(context);
        context.Information($"Configuration:        {context.MsBuildConfiguration}");
        context.EndGroup();
    }

    /// <summary>Stamps the public key and campaign date only for official tagged releases and prereleases.</summary>
    private static void SetTelemetryMetadata(BuildContext context)
    {
        // Only official tagged releases and prereleases collect. Capture once for this build,
        // then reuse the same metadata for every package/runtime produced by Cake.
        var eligible = context.IsTaggedRelease || context.IsTaggedPreRelease;
        var releaseDate = eligible
            ? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;
        var projectKey = eligible ? context.EnvironmentVariable(EnvVars.TelemetryProjectKey) : string.Empty;
        context.MsBuildSettings.WithProperty("GitVersionTelemetryProjectKey", projectKey ?? string.Empty);
        context.MsBuildSettings.WithProperty("GitVersionTelemetryCampaignStart", releaseDate);
    }

    private static void SetMsBuildSettingsVersion(BuildContext context)
    {
        var msBuildSettings = context.MsBuildSettings;
        ArgumentNullException.ThrowIfNull(context.Version);
        var version = context.Version;

        msBuildSettings.SetVersion(version.SemVersion);
        msBuildSettings.SetAssemblyVersion(version.Version);
        msBuildSettings.SetPackageVersion(version.NugetVersion);
        msBuildSettings.SetFileVersion(version.Version);
        msBuildSettings.SetInformationalVersion(version.GitVersion.InformationalVersion);
        msBuildSettings.SetContinuousIntegrationBuild(!context.IsLocalBuild);
        msBuildSettings.WithProperty("RepositoryBranch", version.GitVersion.BranchName);
        msBuildSettings.WithProperty("RepositoryCommit", version.GitVersion.Sha);
        msBuildSettings.WithProperty("NoPackageAnalysis", "true");
        msBuildSettings.WithProperty("UseSharedCompilation", "false");

        // https://github.com/dotnet/docs/issues/37674
        msBuildSettings.WithProperty("IncludeSourceRevisionInInformationalVersion", "false");
    }
}
