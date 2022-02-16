using Xunit;

namespace Common.Utilities;

public enum Architecture
{
    Arm64,
    Amd64
}
public static class DockerContextExtensions
{
    public static bool SkipArm64Image(this ICakeContext context, DockerImage dockerImage)
    {
        if (dockerImage.Architecture != Architecture.Arm64) return false;
        if (!Constants.DistrosToSkip.Contains(dockerImage.Distro)) return false;

        context.Information($"Skipping Target: {dockerImage.TargetFramework}, Distro: {dockerImage.Distro}, Arch: {dockerImage.Architecture}");
        return true;
    }

    public static void DockerBuildImage(this BuildContextBase context, DockerImage dockerImage)
    {
        if (context.Version == null) return;

        var (distro, targetFramework, arch, registry, _) = dockerImage;

        context.Information($"Building image: {dockerImage}");

        var workDir = Paths.Src.Combine("Docker");
        var tags = context.GetDockerTags(dockerImage, arch);

        var suffix = arch.ToSuffix();
        var platforms = new List<string> { $"linux/{suffix}" };

        var buildSettings = new DockerImageBuildSettings
        {
            Rm = true,
            Tag = tags.ToArray(),
            File = workDir.CombineWithFilePath("Dockerfile").FullPath,
            BuildArg = new[]
            {
                $"contentFolder=/content",
                $"REGISTRY={registry}",
                $"DOTNET_VERSION={targetFramework}",
                $"DISTRO={distro}",
                $"VERSION={context.Version.NugetVersion}"
            },
            Pull = true,
            Platform = string.Join(",", platforms),
        };

        context.DockerBuild(buildSettings, workDir.ToString(), "--output type=docker");
    }

    public static void DockerPushImage(this BuildContextBase context, DockerImage dockerImage)
    {
        var tags = context.GetDockerTags(dockerImage, dockerImage.Architecture);
        foreach (var tag in tags)
        {
            context.DockerPush(tag);
        }
    }

    public static void DockerCreateManifest(this BuildContextBase context, DockerImage dockerImage, bool skipArm64Image)
    {
        var manifestTags = context.GetDockerTags(dockerImage);
        foreach (var tag in manifestTags)
        {
            var manifestCreateSettings = new DockerManifestCreateSettings { Amend = true };
            var amd64Tag = $"{tag}-{Architecture.Amd64.ToSuffix()}";
            if (skipArm64Image)
            {
                context.DockerManifestCreate(manifestCreateSettings, tag, amd64Tag);
            }
            else
            {
                var arm64Tag = $"{tag}-{Architecture.Arm64.ToSuffix()}";
                context.DockerManifestCreate(manifestCreateSettings, tag, amd64Tag, arm64Tag);
            }
        }
    }

    public static void DockerPushManifest(this BuildContextBase context, DockerImage dockerImage)
    {
        var manifestTags = context.GetDockerTags(dockerImage);
        foreach (var tag in manifestTags)
        {
            context.DockerManifestPush(new DockerManifestPushSettings { Purge = true }, tag);
        }
    }

    public static void DockerPullImage(this ICakeContext context, DockerImage dockerImage)
    {
        var tag = $"{dockerImage.DockerImageName()}:{dockerImage.Distro}-sdk-{dockerImage.TargetFramework}";
        var platform = $"linux/{dockerImage.Architecture.ToString().ToLower()}";
        context.DockerPull(new DockerImagePullSettings { Platform = platform }, tag);
    }

    public static void DockerTestImage(this BuildContextBase context, DockerImage dockerImage)
    {
        var tags = context.GetDockerTags(dockerImage, dockerImage.Architecture);
        foreach (var tag in tags)
        {
            context.DockerTestRun(tag, dockerImage.Architecture, "/repo", "/showvariable", "FullSemver");
        }
    }

    public static void DockerTestArtifact(this BuildContextBase context, DockerImage dockerImage, string cmd)
    {
        var tag = $"{dockerImage.DockerImageName()}:{dockerImage.Distro}-sdk-{dockerImage.TargetFramework}";
        context.DockerTestRun(tag, dockerImage.Architecture, "sh", cmd);
    }

    private static void DockerBuild(
        this ICakeContext context,
        DockerImageBuildSettings settings,
        string path, params string[] args)
    {
        GenericDockerRunner<DockerImageBuildSettings> genericDockerRunner =
            new(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);

        string str;
        switch (string.IsNullOrEmpty(path))
        {
            case false:
                {
                    string str2 = path.Trim();
                    str = str2.Length <= 1 || !str2.StartsWith("\"") || !str2.EndsWith("\"") ? "\"" + path + "\"" : path;
                    break;
                }
            default:
                str = path;
                break;
        }
        var additional = args.Concat(new[] { str }).ToArray();
        genericDockerRunner.Run("buildx build", settings, additional);
    }

    private static void DockerTestRun(this BuildContextBase context, string image, Architecture arch, string command, params string[] args)
    {
        var settings = GetDockerRunSettings(context, arch);
        context.Information($"Testing image: {image}");
        var output = context.DockerRunImage(settings, image, command, args);
        context.Information("Output : " + output);

        Assert.Contains(context.Version?.GitVersion.FullSemVer, output);
    }
    private static IEnumerable<string> GetDockerTags(this BuildContextBase context, DockerImage dockerImage, Architecture? arch = null)
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

        if (distro == Constants.DockerDistroLatest && targetFramework == Constants.Version60)
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

        if (!arch.HasValue) return tags.Distinct();

        var suffix = arch.Value.ToSuffix();
        return tags.Select(x => $"{x}-{suffix}").Distinct();

    }
    private static string DockerImageName(this DockerImage image) => $"{image.Registry}/{(image.UseBaseImage ? Constants.DockerBaseImageName : Constants.DockerImageName)}";
    private static DockerContainerRunSettings GetDockerRunSettings(this BuildContextBase context, Architecture arch)
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
            },
            Platform = $"linux/{arch.ToString().ToLower()}"
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
