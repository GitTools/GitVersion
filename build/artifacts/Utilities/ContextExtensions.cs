using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Docker;
using Xunit;
using Constants = Common.Utilities.Constants;

namespace Artifacts.Utilities
{
    public static class ContextExtensions
    {
        public static void DockerBuild(this BuildContext context, DockerImage dockerImage)
        {
            var (distro, targetFramework) = dockerImage;
            var workDir = DirectoryPath.FromString($"./src/Docker");
            var tags = context.GetDockerTagsForRepository(dockerImage, Constants.GitHubContainerRegistry);

            if (context.Version == null) return;
            var buildSettings = new DockerImageBuildSettings
            {
                Rm = true,
                Tag = tags.ToArray(),
                File = $"{workDir}/Dockerfile",
                BuildArg = new[]
                {
                    $"contentFolder=/content",
                    $"DOTNET_VERSION={targetFramework}",
                    $"DISTRO={distro}",
                    $"VERSION={context.Version.NugetVersion}"
                },
                // Pull = true,
                // Platform = platform // TODO this one is not supported on docker versions < 18.02
            };

            context.DockerBuild(buildSettings, workDir.ToString());
        }


        public static void DockerPush(this BuildContext context, DockerImage dockerImage, string repositoryName)
        {
            var tags = context.GetDockerTagsForRepository(dockerImage, repositoryName);

            foreach (var tag in tags)
            {
                context.DockerPush(tag);
            }
        }

        public static void DockerTestArtifact(this BuildContext context, DockerImage dockerImage, string cmd, string repositoryName)
        {
            var settings = GetDockerRunSettings(context);
            var (distro, targetFramework) = dockerImage;
            var tag = $"{repositoryName}:{distro}-sdk-{targetFramework}";

            context.Information("Docker tag: {0}", tag);
            context.Information("Docker cmd: pwsh {0}", cmd);

            context.DockerTestRun(settings, tag, "pwsh", cmd);
        }

        public static void DockerPullImage(this ICakeContext context, DockerImage dockerImage, string repositoryName)
        {
            var (distro, targetFramework) = dockerImage;
            var tag = $"{repositoryName}:{distro}-sdk-{targetFramework}";
            context.DockerPull(tag);
        }
        private static DockerContainerRunSettings GetDockerRunSettings(this BuildContext context)
        {
            var currentDir = context.MakeAbsolute(context.Directory("."));
            var root = string.Empty;
            var settings = new DockerContainerRunSettings
            {
                Rm = true,
                Volume = new[]
                {
                    $"{currentDir}:{root}/repo",
                    $"{currentDir}/tests/scripts:{root}/scripts",
                    $"{currentDir}/artifacts/packages/nuget:{root}/nuget",
                    $"{currentDir}/artifacts/packages/native:{root}/native",
                }
            };

            if (context.IsAzurePipelineBuild)
            {
                settings.Env = new[]
                {
                    "TF_BUILD=true",
                    $"BUILD_SOURCEBRANCH={context.EnvironmentVariable("BUILD_SOURCEBRANCH")}"
                };
            }
            if (context.IsGitHubActionsBuild)
            {
                settings.Env = new[]
                {
                    "GITHUB_ACTIONS=true",
                    $"GITHUB_REF={context.EnvironmentVariable("GITHUB_REF")}"
                };
            }

            return settings;
        }

        private static string DockerRunImage(this ICakeContext context, DockerContainerRunSettings settings, string image, string command, params string[] args)
        {
            if (string.IsNullOrEmpty(image))
            {
                throw new ArgumentNullException(nameof(image));
            }
            var runner = new GenericDockerRunner<DockerContainerRunSettings>(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
            List<string> arguments = new() { image };
            if (!string.IsNullOrEmpty(command))
            {
                arguments.Add(command);
                if (args.Length > 0)
                {
                    arguments.AddRange(args);
                }
            }

            var result = runner.RunWithResult("run", settings, r => r.ToArray(), arguments.ToArray());
            return string.Join("\n", result);
        }

        private static void DockerTestRun(this BuildContext context, DockerContainerRunSettings settings, string image, string command, params string[] args)
        {
            context.Information($"Testing image: {image}");
            var output = context.DockerRunImage(settings, image, command, args);
            context.Information("Output : " + output);

            Assert.Equal(context.Version?.GitVersion.FullSemVer, output);
        }
    }
}
