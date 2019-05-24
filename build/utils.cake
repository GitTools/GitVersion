
FilePath FindToolInPath(string tool)
{
    var pathEnv = EnvironmentVariable("PATH");
    if (string.IsNullOrEmpty(pathEnv) || string.IsNullOrEmpty(tool)) return tool;

    var paths = pathEnv.Split(new []{ IsRunningOnUnix() ? ':' : ';'},  StringSplitOptions.RemoveEmptyEntries);
    return paths.Select(path => new DirectoryPath(path).CombineWithFilePath(tool)).FirstOrDefault(filePath => FileExists(filePath.FullPath));
}

DirectoryPath HomePath()
{
    return IsRunningOnWindows()
        ? new DirectoryPath(EnvironmentVariable("HOMEDRIVE") +  EnvironmentVariable("HOMEPATH"))
        : new DirectoryPath(EnvironmentVariable("HOME"));
}

void ReplaceTextInFile(FilePath filePath, string oldValue, string newValue, bool encrypt = false)
{
    Information("Replacing {0} with {1} in {2}", oldValue, !encrypt ? newValue : "******", filePath);
    var file = filePath.FullPath.ToString();
    System.IO.File.WriteAllText(file, System.IO.File.ReadAllText(file).Replace(oldValue, newValue));
}

void SetRubyGemPushApiKey(string apiKey)
{
    // it's a hack, creating a credentials file to be able to push the gem
    var workDir = "./src/GitVersionRubyGem";
    var gemHomeDir = HomePath().Combine(".gem");
    var credentialFile = new FilePath(workDir + "/credentials");
    EnsureDirectoryExists(gemHomeDir);
    ReplaceTextInFile(credentialFile, "$api_key$", apiKey, true);
    CopyFileToDirectory(credentialFile, gemHomeDir);
}

GitVersion GetVersion(BuildParameters parameters)
{
    var dllFile = GetFiles($"**/GitVersionExe/bin/{parameters.Configuration}/{parameters.CoreFxVersion}/GitVersion.dll").FirstOrDefault();
    var settings = new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json,
        ToolPath = FindToolInPath(IsRunningOnUnix() ? "dotnet" : "dotnet.exe"),
        ArgumentCustomization = args => dllFile + " " + args.Render()
    };

    var gitVersion = GitVersion(settings);

    if (!parameters.IsLocalBuild && !(parameters.IsRunningOnAzurePipeline && parameters.IsPullRequest))
    {
        settings.UpdateAssemblyInfo = true;
        settings.LogFilePath = "console";
        settings.OutputType = GitVersionOutput.BuildServer;

        GitVersion(settings);
    }
    return gitVersion;
}

void Build(string configuration)
{
    DotNetCoreRestore("./src/GitVersion.sln");
    MSBuild("./src/GitVersion.sln", settings =>
    {
        settings.SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .WithTarget("Build")
            .WithProperty("POSIX", IsRunningOnUnix().ToString());

        if (IsRunningOnWindows()) {
            settings.ToolPath = GetFiles(VSWhereLatest() + "/MSBuild/**/Bin/MSBuild.exe").First();
        }
    });
}

void ILRepackGitVersionExe(bool includeLibGit2Sharp, DirectoryPath target, DirectoryPath ilMerge, string configuration, string dotnetVersion)
{
    var exeName = "GitVersion.exe";
    var keyFilePath = "./src/key.snk";

    var targetDir = target + "/";
    var ilMergeDir = ilMerge + "/";
    var targetPath     = targetDir + exeName;
    string outFilePath = ilMergeDir + exeName;

    CleanDirectory(ilMergeDir);
    CreateDirectory(ilMergeDir);

    var sourcePattern = targetDir + "*.dll";
    var sourceFiles = GetFiles(sourcePattern);

    if (!includeLibGit2Sharp)
    {
        var excludePattern = "**/LibGit2Sharp.dll";
        sourceFiles = sourceFiles - GetFiles(excludePattern);
    }
    var settings = new ILRepackSettings { AllowDup = "", Keyfile = keyFilePath, Internalize = true, NDebug = true, TargetKind = TargetKind.Exe, TargetPlatform  = TargetPlatformVersion.v4, XmlDocs = false };

    if (IsRunningOnUnix())
    {
        var libFolder = GetDirectories($"**/GitVersionExe/bin/{configuration}/{dotnetVersion}").FirstOrDefault();
        settings.Libs = new List<DirectoryPath> { libFolder };
    }

    ILRepack(outFilePath, targetPath, sourceFiles, settings);

    CopyFileToDirectory("./LICENSE", ilMergeDir);
    CopyFileToDirectory(targetDir + "GitVersion.pdb", ilMergeDir);

    Information("Copying libgit2sharp files..");

    if (!includeLibGit2Sharp) {
        CopyFileToDirectory(targetDir + "LibGit2Sharp.dll", ilMergeDir);
    }
    CopyFileToDirectory(targetDir + "LibGit2Sharp.dll.config", ilMergeDir);
    CopyDirectory(targetDir + "/lib/", ilMergeDir + "/lib/");
}

void PublishILRepackedGitVersionExe(bool includeLibGit2Sharp, DirectoryPath targetDir, DirectoryPath ilMergDir, DirectoryPath outputDir, string configuration, string dotnetVersion)
{
    ILRepackGitVersionExe(includeLibGit2Sharp, targetDir, ilMergDir, configuration, dotnetVersion);
    CopyDirectory(ilMergDir, outputDir);

    if (includeLibGit2Sharp) {
        CopyFiles("./src/GitVersionExe/NugetAssets/*.ps1", outputDir);
    }

    // Copy license & Copy GitVersion.XML (since publish does not do this anymore)
    CopyFileToDirectory("./LICENSE", outputDir);
    CopyFileToDirectory("./src/GitVersionExe/bin/" + configuration + "/" + dotnetVersion + "/GitVersion.xml", outputDir);
}

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

void GetReleaseNotes(FilePath outputPath, DirectoryPath workDir, string repoToken)
{
    var toolPath = Context.Tools.Resolve("GitReleaseNotes.exe");

    var arguments = new ProcessArgumentBuilder()
                .Append(workDir.ToString())
                .Append("/OutputFile")
                .Append(outputPath.ToString())
                .Append("/RepoToken")
                .Append(repoToken);

    StartProcess(toolPath, new ProcessSettings { Arguments = arguments, RedirectStandardOutput = true }, out var redirectedOutput);

    Information(string.Join("\n", redirectedOutput));
}

void UpdateTaskVersion(FilePath taskJsonPath, string taskId, GitVersion gitVersion)
{
    var taskJson = ParseJsonFromFile(taskJsonPath);
    taskJson["id"] = taskId;
    taskJson["version"]["Major"] = gitVersion.Major.ToString();
    taskJson["version"]["Minor"] = gitVersion.Minor.ToString();
    taskJson["version"]["Patch"] = gitVersion.Patch.ToString();
    SerializeJsonToPrettyFile(taskJsonPath, taskJson);
}
