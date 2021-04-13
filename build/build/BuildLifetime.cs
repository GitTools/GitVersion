using System;
using System.Collections.Generic;
using Build.Utilities;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Core;
using Cake.Frosting;
using Cake.Incubator.LoggingExtensions;
using Common.Addins.GitVersion;
using Common.Utilities;

namespace Build
{
    public class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            context.StartGroup("Build Setup");

            context.MsBuildConfiguration = context.Argument(Arguments.Configuration, "Release");

            var buildSystem = context.BuildSystem();
            context.IsLocalBuild = buildSystem.IsLocalBuild;
            context.IsAppVeyorBuild = buildSystem.IsRunningOnAppVeyor;
            context.IsAzurePipelineBuild = buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted;
            context.IsGitHubActionsBuild = buildSystem.IsRunningOnGitHubActions;

            context.IsPullRequest = buildSystem.IsPullRequest;
            context.IsOriginalRepo = context.IsOriginalRepo();
            context.IsMainBranch = context.IsMainBranch();
            context.IsTagged = context.IsTagged();

            context.IsOnWindows = context.IsRunningOnWindows();
            context.IsOnLinux = context.IsRunningOnLinux();
            context.IsOnMacOS = context.IsRunningOnMacOs();

            context.EnabledUnitTests = context.IsEnabled(EnvVars.EnabledUnitTests);

            context.Information("Configuration:     {0}", context.MsBuildConfiguration);
            context.Information("Build Agent:       {0}", context.GetBuildAgent());
            context.Information("OS:                {0}", context.GetOS());
            context.Information("Pull Request:      {0}", context.IsPullRequest);
            context.Information("Original Repo:     {0}", context.IsOriginalRepo);
            context.Information("Main Branch:       {0}", context.IsMainBranch);
            context.Information("Tagged:            {0}", context.IsTagged);
            context.EndGroup();

            var gitVersion = context.GitVersion(new GitVersionSettings
            {
                OutputTypes = new HashSet<GitVersionOutput> { GitVersionOutput.Json, GitVersionOutput.BuildServer }
            });

            context.Version = BuildVersion.Calculate(gitVersion);
            context.Credentials = BuildCredentials.GetCredentials(context);

            context.Packages = BuildPackages.GetPackages(
                Paths.Nuget,
                context.Version,
                new[] { "GitVersion.CommandLine", "GitVersion.Core", "GitVersion.MsBuild", "GitVersion.Tool" },
                new[] { "GitVersion.Portable" });

            SetMsBuildSettingsVersion(context.MsBuildSettings, context.Version);
        }

        private static void SetMsBuildSettingsVersion(DotNetCoreMSBuildSettings msBuildSettings, BuildVersion version)
        {
            msBuildSettings.WithProperty("Version", version.SemVersion);
            msBuildSettings.WithProperty("AssemblyVersion", version.Version);
            msBuildSettings.WithProperty("PackageVersion", version.NugetVersion);
            msBuildSettings.WithProperty("FileVersion", version.Version);
            msBuildSettings.WithProperty("InformationalVersion", version.GitVersion?.InformationalVersion);
            msBuildSettings.WithProperty("RepositoryBranch", version.GitVersion?.BranchName);
            msBuildSettings.WithProperty("RepositoryCommit", version.GitVersion?.Sha);
            msBuildSettings.WithProperty("NoPackageAnalysis", "true");
        }

        public override void Teardown(BuildContext context, ITeardownContext info)
        {
            context.StartGroup("Build Teardown");
            try
            {
                context.Information("Starting Teardown...");

                context.Information("Pull Request:      {0}", context.IsPullRequest);
                context.Information("Original Repo:     {0}", context.IsOriginalRepo);
                context.Information("Main Branch:       {0}", context.IsMainBranch);
                context.Information("Tagged:            {0}", context.IsTagged);

                context.Information("Finished running tasks.");
            }
            catch (Exception exception)
            {
                context.Error(exception.Dump());
            }
            context.EndGroup();
        }
    }

}
