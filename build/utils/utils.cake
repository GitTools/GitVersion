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
    var dllFilePath = $"./artifacts/*/bin/{parameters.CoreFxVersion21}/tools/GitVersion.dll";
    var dllFile = GetFiles(dllFilePath).FirstOrDefault();
    if (dllFile == null)
    {
        Warning("Dogfood GitVersion to get information");
        Build(parameters);
        dllFilePath = $"./src/GitVersionExe/bin/{parameters.Configuration}/{parameters.CoreFxVersion21}/GitVersion.dll";
        dllFile = GetFiles(dllFilePath).FirstOrDefault();
    }

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

void Build(BuildParameters parameters)
{
    var sln = "./src/GitVersion.sln";
    DotNetCoreRestore(sln, new DotNetCoreRestoreSettings
    {
        Verbosity = DotNetCoreVerbosity.Minimal,
        Sources = new [] { "https://api.nuget.org/v3/index.json" },
        MSBuildSettings = parameters.MSBuildSettings
    });

    var slnPath = MakeAbsolute(new DirectoryPath(sln));
    DotNetCoreBuild(slnPath.FullPath, new DotNetCoreBuildSettings
    {
        Verbosity = DotNetCoreVerbosity.Minimal,
        Configuration = parameters.Configuration,
        NoRestore = true,
        MSBuildSettings = parameters.MSBuildSettings
    });
}

void UpdateTaskVersion(FilePath taskJsonPath, string taskId, string titleSuffix, GitVersion gitVersion)
{
    var taskJson = ParseJsonFromFile(taskJsonPath);
    taskJson["id"] = taskId;
    taskJson["friendlyName"] += titleSuffix;
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

public static List<string> ExecGitCmd(ICakeContext context, string cmd)
{
    var gitPath = context.Tools.Resolve(context.IsRunningOnWindows() ? "git.exe" : "git");
    context.StartProcess(gitPath, new ProcessSettings { Arguments = cmd, RedirectStandardOutput = true }, out var redirectedOutput);

    return redirectedOutput.ToList();
}
