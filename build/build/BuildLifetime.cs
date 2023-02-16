using Build.Utilities;
using Common.Lifetime;
using Common.Utilities;

namespace Build;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        base.Setup(context, info);

        context.MsBuildConfiguration = context.Argument(Arguments.Configuration, Constants.DefaultConfiguration);
        context.EnabledUnitTests = context.IsEnabled(EnvVars.EnabledUnitTests);

        context.Credentials = Credentials.GetCredentials(context);

        if (context.Version is not null)
        {
            SetMsBuildSettingsVersion(context);
        }

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
        msBuildSettings.WithProperty("UseSharedCompilation", "false");
    }
}
