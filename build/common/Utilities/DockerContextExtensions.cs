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

namespace Common.Utilities
{
    public static class DockerContextExtensions
    {
        public static void DockerBuild(this BuildContextBase context, DockerImage dockerImage)
        {
            var (distro, targetFramework, registry, _) = dockerImage;
            var workDir = DirectoryPath.FromString($"./src/Docker");
            var tags = context.GetDockerTags(dockerImage);

            if (context.Version == null) return;
            var buildSettings = new DockerImageBuildSettings
            {
                Rm = true,
                Tag = tags.ToArray(),
                File = $"{workDir}/Dockerfile",
                BuildArg = new[]
                {
                    $"contentFolder=/content",
                    $"REGISTRY={registry}",
                    $"DOTNET_VERSION={targetFramework}",
                    $"DISTRO={distro}",
                    $"VERSION={context.Version.NugetVersion}"
                },
                // Pull = true,
                // Platform = platform // TODO this one is not supported on docker versions < 18.02
            };

            context.DockerBuild(buildSettings, workDir.ToString());
        }

        public static void DockerPush(this BuildContextBase context, DockerImage dockerImage)
        {
            var tags = context.GetDockerTags(dockerImage);

            foreach (var tag in tags)
            {
                context.DockerPush(tag);
            }
        }

        public static void DockerPullImage(this ICakeContext context, DockerImage dockerImage)
        {
            var tag = $"{dockerImage.DockerImageName()}:{dockerImage.Distro}-sdk-{dockerImage.TargetFramework}";
            context.DockerPull(tag);
        }

        public static void DockerTestImage(this BuildContextBase context, DockerImage dockerImage)
        {
            var tags = context.GetDockerTags(dockerImage);
            foreach (var tag in tags)
            {
                context.DockerTestRun(tag, "/repo", "/showvariable", "FullSemver");
            }
        }

        public static void DockerTestArtifact(this BuildContextBase context, DockerImage dockerImage, string cmd)
        {
            var tag = $"{dockerImage.DockerImageName()}:{dockerImage.Distro}-sdk-{dockerImage.TargetFramework}";
            context.DockerTestRun(tag, "pwsh", cmd);
        }

        private static void DockerTestRun(this BuildContextBase context, string image, string command, params string[] args)
        {
            var settings = GetDockerRunSettings(context);
            context.Information($"Testing image: {image}");
            var output = context.DockerRunImage(settings, image, command, args);
            context.Information("Output : " + output);

            Assert.Equal(context.Version?.GitVersion.FullSemVer, output);
        }
        private static IEnumerable<string> GetDockerTags(this BuildContextBase context, DockerImage dockerImage)
        {
            var name = dockerImage.DockerImageName();
            var distro = dockerImage.Distro;
            var targetFramework = dockerImage.TargetFramework;

            if (context.Version == null) return Enumerable.Empty<string>();
            var tags = new List<string>
            {
                $"{name}:{context.Version.Version}-{distro}-{targetFramework}",
                $"{name}:{context.Version.SemVersion}-{distro}-{targetFramework}",
            };

            if (distro == Constants.DockerDistroLatest && targetFramework == Constants.Version50)
            {
                tags.AddRange(new[]
                {
                    $"{name}:{context.Version.Version}",
                    $"{name}:{context.Version.SemVersion}",

                    $"{name}:{context.Version.Version}-{distro}",
                    $"{name}:{context.Version.SemVersion}-{distro}"
                });

                if (context.IsStableRelease)
                {
                    tags.AddRange(new[]
                    {
                        $"{name}:latest",
                        $"{name}:latest-{targetFramework}",
                        $"{name}:latest-{distro}",
                        $"{name}:latest-{distro}-{targetFramework}",
                    });
                }
            }

            return tags;
        }
        private static string DockerImageName(this DockerImage image) => $"{image.Registry}/{(image.UseBaseImage ? Constants.DockerBaseImageName : Constants.DockerImageName)}";
        private static DockerContainerRunSettings GetDockerRunSettings(this BuildContextBase context)
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
            List<string> arguments = new()
            {
                image
            };
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
    }
}
