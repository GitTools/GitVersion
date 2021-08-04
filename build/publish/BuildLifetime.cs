using System;
using System.Collections.Generic;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Cake.Incubator.LoggingExtensions;
using Common.Addins.GitVersion;
using Common.Utilities;
using Publish.Utilities;

namespace Publish
{
    public class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            context.StartGroup("Build Setup");

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

            var gitVersion = context.GitVersion(new GitVersionSettings
            {
                OutputTypes = new HashSet<GitVersionOutput> { GitVersionOutput.Json, GitVersionOutput.BuildServer }
            });

            context.Version = BuildVersion.Calculate(gitVersion);
            context.Credentials = BuildCredentials.GetCredentials(context);

            context.Information("Version:           {0}", context.Version.SemVersion);
            context.Information("Build Agent:       {0}", context.GetBuildAgent());
            context.Information("OS:                {0}", context.GetOS());
            context.Information("Pull Request:      {0}", context.IsPullRequest);
            context.Information("Original Repo:     {0}", context.IsOriginalRepo);
            context.Information("Main Branch:       {0}", context.IsMainBranch);
            context.Information("Tagged:            {0}", context.IsTagged);
            context.EndGroup();
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
