#load "./docker.cake"

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
    Build(parameters.Configuration);
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

void UpdateTaskVersion(FilePath taskJsonPath, string taskId, GitVersion gitVersion)
{
    var taskJson = ParseJsonFromFile(taskJsonPath);
    taskJson["id"] = taskId;
    taskJson["version"]["Major"] = gitVersion.Major.ToString();
    taskJson["version"]["Minor"] = gitVersion.Minor.ToString();
    taskJson["version"]["Patch"] = gitVersion.Patch.ToString();
    SerializeJsonToPrettyFile(taskJsonPath, taskJson);
}

public static CakeTaskBuilder IsDependentOnWhen(this CakeTaskBuilder builder, string name, bool condition)
{
    if (builder == null)
    {
        throw new ArgumentNullException(nameof(builder));
    }
    if (condition)
    {
        builder.IsDependentOn(name);
    }
    return builder;
}

public static bool IsEnabled(ICakeContext context, string envVar, bool nullOrEmptyAsEnabled = true)
{
    var value = context.EnvironmentVariable(envVar);

    return string.IsNullOrWhiteSpace(value) ? nullOrEmptyAsEnabled : bool.Parse(value);
}
