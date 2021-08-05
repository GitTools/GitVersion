using Build.Utilities;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Common.Utilities;

namespace Build
{
    public class BuildLifetime : BuildLifetimeBase<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            base.Setup(context);

            context.MsBuildConfiguration = context.Argument(Arguments.Configuration, "Release");
            context.EnabledUnitTests = context.IsEnabled(EnvVars.EnabledUnitTests);

            context.Credentials = BuildCredentials.GetCredentials(context);

            SetMsBuildSettingsVersion(context.MsBuildSettings, context.Version!);

            context.StartGroup("Build Setup");
            LogBuildInformation(context);
            context.Information("Configuration:     {0}", context.MsBuildConfiguration);
            context.EndGroup();
        }

        private static void SetMsBuildSettingsVersion(DotNetCoreMSBuildSettings msBuildSettings, BuildVersion version)
        {
            msBuildSettings.WithProperty("Version", version.SemVersion);
            msBuildSettings.WithProperty("AssemblyVersion", version.Version);
            msBuildSettings.WithProperty("PackageVersion", version.NugetVersion);
            msBuildSettings.WithProperty("FileVersion", version.Version);
            msBuildSettings.WithProperty("InformationalVersion", version.GitVersion.InformationalVersion);
            msBuildSettings.WithProperty("RepositoryBranch", version.GitVersion.BranchName);
            msBuildSettings.WithProperty("RepositoryCommit", version.GitVersion.Sha);
            msBuildSettings.WithProperty("NoPackageAnalysis", "true");
        }
    }
}
