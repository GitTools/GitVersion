using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Docker;
using Cake.Frosting;
using Cake.Incubator.LoggingExtensions;
using Common.Addins.GitVersion;
using Common.Utilities;
using Docker.Utilities;
using Constants = Common.Utilities.Constants;

namespace Docker
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

            context.IsDockerOnLinux = context.DockerCustomCommand("info --format '{{.OSType}}'").First().Replace("'", "") == "linux";

            var gitVersion = context.GitVersion(new GitVersionSettings
            {
                OutputTypes = new HashSet<GitVersionOutput> { GitVersionOutput.Json, GitVersionOutput.BuildServer }
            });

            context.Version = BuildVersion.Calculate(gitVersion);
            context.Credentials = BuildCredentials.GetCredentials(context);

            var dotnetVersion = context.Argument("docker_dotnetversion", "").ToLower();
            var dockerDistro = context.Argument("dotnet_distro", "").ToLower();

            context.Information($"Building for Version: {dotnetVersion}, Distro: {dockerDistro}");

            var versions = string.IsNullOrWhiteSpace(dotnetVersion) ? Constants.VersionsToBuild : new[] { dotnetVersion };
            var distros = string.IsNullOrWhiteSpace(dockerDistro) ? Constants.DockerDistrosToBuild : new[] { dockerDistro };

            context.Images = from version in versions
                             from distro in distros
                             select new DockerImage(distro, version);

            context.Information("Version:           {0}", context.Version.SemVersion);
            context.Information("Build Agent:       {0}", context.GetBuildAgent());
            context.Information("OS:                {0}", context.GetOS());
            context.Information("Pull Request:      {0}", context.IsPullRequest);
            context.Information("Original Repo:     {0}", context.IsOriginalRepo);
            context.Information("Main Branch:       {0}", context.IsMainBranch);
            context.Information("Tagged:            {0}", context.IsTagged);
            context.Information("IsDockerOnLinux:   {0}", context.IsDockerOnLinux);
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
