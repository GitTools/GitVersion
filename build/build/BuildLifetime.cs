using System;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Cake.Incubator.LoggingExtensions;
using Common.Utilities;

namespace Build
{
    public class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            context.StartGroup("Build Setup");

            context.Target = context.Argument("target", "Default");
            context.Configuration = context.Argument("configuration", "Release");

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

            context.Information("Configuration:     {0}", context.Configuration);
            context.Information("Target:            {0}", context.Target);
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
