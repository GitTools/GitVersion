#load "./docker.cake"

public static FilePath FindToolInPath(this ICakeContext context, string tool)
{
    var pathEnv = context.EnvironmentVariable("PATH");
    if (string.IsNullOrEmpty(pathEnv) || string.IsNullOrEmpty(tool)) return tool;

    var paths = pathEnv.Split(new []{ context.IsRunningOnUnix() ? ':' : ';'},  StringSplitOptions.RemoveEmptyEntries);
    return paths.Select(path => new DirectoryPath(path).CombineWithFilePath(tool)).FirstOrDefault(filePath => context.FileExists(filePath.FullPath));
}

public static bool IsOnMainRepo(this ICakeContext context)
{
    var buildSystem = context.BuildSystem();
    string repositoryName = null;
    if (buildSystem.IsRunningOnAppVeyor)
    {
        repositoryName = buildSystem.AppVeyor.Environment.Repository.Name;
    }
    else if (buildSystem.IsRunningOnTravisCI)
    {
        repositoryName = buildSystem.TravisCI.Environment.Repository.Slug;
    }
    else if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted)
    {
        repositoryName = buildSystem.TFBuild.Environment.Repository.RepoName;
    }
    else if (buildSystem.IsRunningOnGitHubActions)
    {
        repositoryName = buildSystem.GitHubActions.Environment.Workflow.Repository;
    }

    context.Information("Repository Name: {0}" , repositoryName);

    return !string.IsNullOrWhiteSpace(repositoryName) && StringComparer.OrdinalIgnoreCase.Equals($"{BuildParameters.MainRepoOwner}/{BuildParameters.MainRepoName}", repositoryName);
}

public static bool IsOnMainBranch(this ICakeContext context)
{
    var buildSystem = context.BuildSystem();
    string repositoryBranch = ExecGitCmd(context, "rev-parse --abbrev-ref HEAD").Single();
    if (buildSystem.IsRunningOnAppVeyor)
    {
        repositoryBranch = buildSystem.AppVeyor.Environment.Repository.Branch;
    }
    else if (buildSystem.IsRunningOnTravisCI)
    {
        repositoryBranch = buildSystem.TravisCI.Environment.Build.Branch;
    }
    else if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted)
    {
        repositoryBranch = buildSystem.TFBuild.Environment.Repository.SourceBranchName;
    }
    else if (buildSystem.IsRunningOnGitHubActions)
    {
        repositoryBranch = buildSystem.GitHubActions.Environment.Workflow.Ref.Replace("refs/heads/", "");
    }

    context.Information("Repository Branch: {0}" , repositoryBranch);

    return !string.IsNullOrWhiteSpace(repositoryBranch) && StringComparer.OrdinalIgnoreCase.Equals("master", repositoryBranch);
}

public static bool IsBuildTagged(this ICakeContext context)
{
    var sha = ExecGitCmd(context, "rev-parse --verify HEAD").Single();
    var isTagged = ExecGitCmd(context, "tag --points-at " + sha).Any();

    return isTagged;
}

public static bool IsEnabled(this ICakeContext context, string envVar, bool nullOrEmptyAsEnabled = true)
{
    var value = context.EnvironmentVariable(envVar);

    return string.IsNullOrWhiteSpace(value) ? nullOrEmptyAsEnabled : bool.Parse(value);
}

public static List<string> ExecuteCommand(this ICakeContext context, FilePath exe, string args)
{
    context.StartProcess(exe, new ProcessSettings { Arguments = args, RedirectStandardOutput = true }, out var redirectedOutput);

    return redirectedOutput.ToList();
}

public static List<string> ExecGitCmd(this ICakeContext context, string cmd)
{
    var gitExe = context.Tools.Resolve(context.IsRunningOnWindows() ? "git.exe" : "git");
    return context.ExecuteCommand(gitExe, cmd);
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

GitVersion GetVersion(BuildParameters parameters)
{
    var gitversionFilePath = $"artifacts/gitversion.json";
    var gitversionFile = GetFiles(gitversionFilePath).FirstOrDefault();
    GitVersion gitVersion = null;
    if (gitversionFile == null || parameters.IsLocalBuild)
    {
        Build(parameters);

        var settings = new GitVersionSettings { OutputType = GitVersionOutput.Json };
        SetGitVersionTool(settings, parameters);

        gitVersion = GitVersion(settings);
        SerializeJsonToPrettyFile(gitversionFilePath, gitVersion);
    }
    else
    {
        gitVersion = DeserializeJsonFromFile<GitVersion>(gitversionFile);
    }

    return gitVersion;
}

void ValidateVersion(BuildParameters parameters)
{
    var gitversionTool = GetGitVersionToolLocation(parameters);

    ValidateOutput("dotnet", $"\"{gitversionTool}\" -version", parameters.Version.GitVersion.InformationalVersion);
}

void ValidateOutput(string cmd, string args, string expected)
{
    var output = Context.ExecuteCommand(cmd, args);
    var outputStr = string.Concat(output);
    Information(outputStr);

    Assert.Equal(expected, outputStr);
}

void RunGitVersionOnCI(BuildParameters parameters)
{
    // set the CI build version number with GitVersion
    if (!parameters.IsLocalBuild)
    {
        var settings = new GitVersionSettings
        {
            LogFilePath = "console",
            OutputType = GitVersionOutput.BuildServer
        };
        SetGitVersionTool(settings, parameters);

        GitVersion(settings);
    }
}

GitVersionSettings SetGitVersionTool(GitVersionSettings settings, BuildParameters parameters)
{
    var gitversionTool = GetGitVersionToolLocation(parameters);

    settings.ToolPath = Context.FindToolInPath(IsRunningOnUnix() ? "dotnet" : "dotnet.exe");
    settings.ArgumentCustomization = args => gitversionTool + " " + args.Render();

    return settings;
}

FilePath GetGitVersionToolLocation(BuildParameters parameters)
{
    return GetFiles($"src/GitVersionExe/bin/{parameters.Configuration}/{parameters.CoreFxVersion31}/gitversion.dll").SingleOrDefault();
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
