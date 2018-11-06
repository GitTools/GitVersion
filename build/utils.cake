
FilePath FindToolInPath(string tool)
{
    var pathEnv = EnvironmentVariable("PATH");
    if (string.IsNullOrEmpty(pathEnv) || string.IsNullOrEmpty(tool)) return tool;

    var paths = pathEnv.Split(new []{ IsRunningOnUnix() ? ':' : ';'},  StringSplitOptions.RemoveEmptyEntries);
    return paths.Select(path => new DirectoryPath(path).CombineWithFilePath(tool)).FirstOrDefault(filePath => FileExists(filePath.FullPath));
}

void FixForMono(Cake.Core.Tooling.ToolSettings toolSettings, string toolExe)
{
    if (IsRunningOnUnix())
    {
        var toolPath = Context.Tools.Resolve(toolExe);
        toolSettings.ToolPath = FindToolInPath("mono");
        toolSettings.ArgumentCustomization = args => toolPath.FullPath + " " + args.Render();
    }
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
    var dllFile = GetFiles($"**/{parameters.NetCoreVersion}/GitVersion.dll").FirstOrDefault();
    var settings = new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json,
        ToolPath = FindToolInPath(IsRunningOnUnix() ? "dotnet" : "dotnet.exe"),
        ArgumentCustomization = args => dllFile + " " + args.Render()
    };

    var gitVersion = GitVersion(settings);

    if (!(parameters.IsRunningOnAzurePipeline && parameters.IsPullRequest))
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
    });
}

void ILRepackGitVersionExe(bool includeLibGit2Sharp, DirectoryPath target, DirectoryPath ilMerge)
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
    FixForMono(settings, "ILRepack.exe");
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
    ILRepackGitVersionExe(includeLibGit2Sharp, targetDir, ilMergDir);
    CopyDirectory(ilMergDir, outputDir);

    if (includeLibGit2Sharp) {
        CopyFiles("./src/GitVersionExe/NugetAssets/*.ps1", outputDir);
    }

    // Copy license & Copy GitVersion.XML (since publish does not do this anymore)
    CopyFileToDirectory("./LICENSE", outputDir);
    CopyFileToDirectory("./src/GitVersionExe/bin/" + configuration + "/" + dotnetVersion + "/GitVersion.xml", outputDir);
}

void DockerBuild(string platform, string variant, BuildParameters parameters)
{
    var workDir = DirectoryPath.FromString($"./src/Docker/{platform}/{variant}");

    var sourceDir =  variant == "dotnetcore"
        ? parameters.Paths.Directories.ArtifactsBinNetCore.Combine("tools")
        : parameters.Paths.Directories.ArtifactsBinFullFxCmdline.Combine("tools");

    CopyDirectory(sourceDir, workDir.Combine("content"));

    var tags = GetDockerTags(platform, variant, parameters);

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

void DockerPush(string platform, string variant, BuildParameters parameters)
{
    var tags = GetDockerTags(platform, variant, parameters);

    foreach (var tag in tags)
    {
        DockerPush(tag);
    }
}

string[] GetDockerTags(string platform, string variant, BuildParameters parameters) {
    var name = $"gittools/gitversion-{variant}";

    var tags = new List<string> {
        $"{name}:{platform}",
        $"{name}:{platform}-{parameters.Version.Version}"
    };

    tags.Add($"{name}:{platform}-{parameters.Version.SemVersion}");

    if (variant == "dotnetcore" && parameters.IsStableRelease()) {
        tags.Add($"{name}:latest");
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

void UpdateTaskVersion(FilePath taskJsonPath, GitVersion gitVersion)
{
    var taskJson = ParseJsonFromFile(taskJsonPath);
    taskJson["version"]["Major"] = gitVersion.Major.ToString();
    taskJson["version"]["Minor"] = gitVersion.Minor.ToString();
    taskJson["version"]["Patch"] = gitVersion.Patch.ToString();
    SerializeJsonToPrettyFile(taskJsonPath, taskJson);
}
