using System;
using System.Collections.Generic;
using System.Linq;
using Artifacts.Utilities;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Docker;
using Cake.Frosting;
using Cake.Incubator.LoggingExtensions;
using Common.Addins.GitVersion;
using Common.Utilities;

namespace Artifacts
{
    public class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public static readonly string[] DockerDistrosToBuild =
        {
            "alpine.3.12-x64",
            "centos.7-x64",
            "centos.8-x64",
            "debian.9-x64",
            "debian.10-x64",
            "fedora.33-x64",
            "ubuntu.16.04-x64",
            "ubuntu.18.04-x64",
            "ubuntu.20.04-x64"
        };

        public static readonly string[] VersionsToBuild = { "5.0", "3.1" };


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

            context.Information("Build Agent:       {0}", context.GetBuildAgent());
            context.Information("OS:                {0}", context.GetOS());
            context.Information("Pull Request:      {0}", context.IsPullRequest);
            context.Information("Original Repo:     {0}", context.IsOriginalRepo);
            context.Information("Main Branch:       {0}", context.IsMainBranch);
            context.Information("Tagged:            {0}", context.IsTagged);
            context.Information("IsDockerOnLinux:   {0}", context.IsDockerOnLinux);
            context.EndGroup();

            var gitVersion = context.GitVersion(new GitVersionSettings
            {
                OutputTypes = new HashSet<GitVersionOutput> { GitVersionOutput.Json, GitVersionOutput.BuildServer }
            });

            context.Version = BuildVersion.Calculate(gitVersion);
            context.Credentials = BuildCredentials.GetCredentials(context);

            var dotnetVersion = context.Argument("docker_dotnetversion", "").ToLower();
            var dockerDistro = context.Argument("dotnet_distro", "").ToLower();

            context.Information($"Building for Version: {dotnetVersion}, Distro: {dockerDistro}");

            var versions = string.IsNullOrWhiteSpace(dotnetVersion) ? VersionsToBuild : new[] { dotnetVersion };
            var distros = string.IsNullOrWhiteSpace(dockerDistro) ? DockerDistrosToBuild : new[] { dockerDistro };

            context.Images = from version in versions
                             from distro in distros
                             select new DockerImage(distro, version);

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
