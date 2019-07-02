void DockerStdinLogin(string username, string password)
{
    var toolPath = FindToolInPath(IsRunningOnUnix() ? "docker" : "docker.exe");
    var args = new ProcessArgumentBuilder()
        .Append("login")
        .Append("--username").AppendQuoted(username)
        .Append("--password-stdin");

    var processStartInfo = new ProcessStartInfo(toolPath.ToString(), args.Render())
    {
        RedirectStandardInput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    using (var process = new Process { StartInfo = processStartInfo })
    {
        process.Start();
        process.StandardInput.WriteLine(password);
        process.StandardInput.Close();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new Exception(toolPath.GetFilename() + " returned exit code " + process.ExitCode + ".");
    }
}

void DockerBuild(DockerImage dockerImage, BuildParameters parameters)
{
    var (os, distro, targetframework) = dockerImage;
    var workDir = DirectoryPath.FromString($"./src/Docker/{targetframework}/{os}/{distro}");

    var sourceDir = targetframework.StartsWith("netcoreapp")
        ? parameters.Paths.Directories.ArtifactsBinCoreFx.Combine("tools")
        : parameters.Paths.Directories.ArtifactsBinFullFxCmdline.Combine("tools");

    CopyDirectory(sourceDir, workDir.Combine("content"));

    var tags = GetDockerTags(dockerImage, parameters);

    var buildSettings = new DockerImageBuildSettings
    {
        Rm = true,
        Tag = tags,
        File = $"{workDir}/Dockerfile",
        BuildArg = new []{ $"contentFolder=/content" },
        // Pull = true,
        // Platform = platform // TODO this one is not supported on docker versions < 18.02
    };

    DockerBuild(buildSettings, workDir.ToString());
}

void DockerPush(DockerImage dockerImage, BuildParameters parameters)
{
    var tags = GetDockerTags(dockerImage, parameters);

    foreach (var tag in tags)
    {
        DockerPush(tag);
    }
}

string DockerRunImage(DockerContainerRunSettings settings, string image, string command, params string[] args)
{
    if (string.IsNullOrEmpty(image))
    {
        throw new ArgumentNullException("image");
    }
    var runner = new GenericDockerRunner<DockerContainerRunSettings>(Context.FileSystem, Context.Environment, Context.ProcessRunner, Context.Tools);
    List<string> arguments = new List<string> { image };
    if (!string.IsNullOrEmpty(command))
    {
        arguments.Add(command);
        if (args.Length > 0)
        {
            arguments.AddRange(args);
        }
    }

    var result = runner.RunWithResult("run", settings ?? new DockerContainerRunSettings(), r => r.ToArray(), arguments.ToArray());
    return string.Join("\n", result);
}

void DockerTestRun(DockerContainerRunSettings settings, BuildParameters parameters, string image, string command, params string[] args)
{
    Information($"Testing image: {image}");
    var output = DockerRunImage(settings, image, command, args);

    var version = DeserializeJson<GitVersion>(output);

    Assert.Equal(parameters.Version.GitVersion.FullSemVer, version.FullSemVer);
}

string[] GetDockerTags(DockerImage dockerImage, BuildParameters parameters) {
    var name = $"gittools/gitversion";
    var (os, distro, targetframework) = dockerImage;

    var tags = new List<string> {
        $"{name}:{parameters.Version.Version}-{os}-{distro}-{targetframework}",
        $"{name}:{parameters.Version.SemVersion}-{os}-{distro}-{targetframework}",
    };

    if (distro == "debian" && targetframework == parameters.CoreFxVersion || distro == "nano") {
        tags.AddRange(new[] {
            $"{name}:{parameters.Version.Version}-{os}",
            $"{name}:{parameters.Version.SemVersion}-{os}",

            $"{name}:{parameters.Version.Version}-{targetframework}",
            $"{name}:{parameters.Version.SemVersion}-{targetframework}",

            $"{name}:{parameters.Version.Version}-{os}-{targetframework}",
            $"{name}:{parameters.Version.SemVersion}-{os}-{targetframework}",
        });

        if (parameters.IsStableRelease())
        {
            tags.AddRange(new[] {
                $"{name}:latest",
                $"{name}:latest-{os}",
                $"{name}:latest-{targetframework}",
                $"{name}:latest-{os}-{targetframework}",
                $"{name}:latest-{os}-{distro}-{targetframework}",
            });
        }
    }

    return tags.ToArray();
}

public static string GetDockerCliPlatform(ICakeContext context) => context.DockerCustomCommand("info --format '{{.OSType}}'").First().Replace("'", "");
