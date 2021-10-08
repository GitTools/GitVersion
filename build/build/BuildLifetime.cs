using Build.Utilities;
using Common.Utilities;

namespace Build;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    public override void Setup(BuildContext context)
    {
        base.Setup(context);

        context.MsBuildConfiguration = context.Argument(Arguments.Configuration, "Release");
        context.EnabledUnitTests = context.IsEnabled(EnvVars.EnabledUnitTests);

        context.Credentials = Credentials.GetCredentials(context);

        SetMsBuildSettingsVersion(context);

        context.StartGroup("Build Setup");
        LogBuildInformation(context);
        context.Information("Configuration:     {0}", context.MsBuildConfiguration);
        context.EndGroup();
    }

    private static void SetMsBuildSettingsVersion(BuildContext context)
    {
        var msBuildSettings = context.MsBuildSettings;
        var version = context.Version!;

        msBuildSettings.SetVersion(version.SemVersion);
        msBuildSettings.SetAssemblyVersion(version.Version);
        msBuildSettings.SetPackageVersion(version.NugetVersion);
        msBuildSettings.SetFileVersion(version.Version);
        msBuildSettings.SetInformationalVersion(version.GitVersion.InformationalVersion);
        msBuildSettings.SetContinuousIntegrationBuild(!context.IsLocalBuild);
        msBuildSettings.WithProperty("RepositoryBranch", version.GitVersion.BranchName);
        msBuildSettings.WithProperty("RepositoryCommit", version.GitVersion.Sha);
        msBuildSettings.WithProperty("NoPackageAnalysis", "true");
    }
}
