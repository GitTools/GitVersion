using Xunit;

namespace Common.Utilities;

public static class ContextExtensions
{
    public static IEnumerable<string> ExecuteCommand(this ICakeContext context, FilePath exe, string? args, DirectoryPath? workDir = null)
    {
        var processSettings = new ProcessSettings { Arguments = args, RedirectStandardOutput = true };
        if (workDir is not null)
        {
            processSettings.WorkingDirectory = workDir;
        }
        context.StartProcess(exe, processSettings, out var redirectedOutput);
        return redirectedOutput.ToList();
    }

    private static IEnumerable<string> ExecGitCmd(this ICakeContext context, string? cmd, DirectoryPath? workDir = null)
    {
        var gitExe = context.Tools.Resolve(context.IsRunningOnWindows() ? "git.exe" : "git");
        return context.ExecuteCommand(gitExe, cmd, workDir);
    }

    public static void GitPushBranch(this ICakeContext context, DirectoryPath repositoryDirectoryPath, string username, string token, string branchName)
    {
        var pushUrl = $"https://{username}:{token}@github.com/{Constants.RepoOwner}/{Constants.Repository}";
        context.ExecGitCmd($"push {pushUrl} {branchName}", workDir: repositoryDirectoryPath);
    }

    public static bool IsOriginalRepo(this ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        string repositoryName = string.Empty;
        if (buildSystem.IsRunningOnAppVeyor)
        {
            repositoryName = buildSystem.AppVeyor.Environment.Repository.Name;
        }
        else if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted)
        {
            repositoryName = buildSystem.AzurePipelines.Environment.Repository.RepoName;
        }
        else if (buildSystem.IsRunningOnGitHubActions)
        {
            repositoryName = buildSystem.GitHubActions.Environment.Workflow.Repository;
        }
        context.Information("Repository Name:   {0}", repositoryName);

        return !string.IsNullOrWhiteSpace(repositoryName) && StringComparer.OrdinalIgnoreCase.Equals("gittools/GitVersion", repositoryName);
    }

    public static bool IsMainBranch(this ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        string repositoryBranch = context.ExecGitCmd("rev-parse --abbrev-ref HEAD").Single();
        if (buildSystem.IsRunningOnAppVeyor)
        {
            repositoryBranch = buildSystem.AppVeyor.Environment.Repository.Branch;
        }
        else if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted)
        {
            repositoryBranch = buildSystem.AzurePipelines.Environment.Repository.SourceBranchName;
        }
        else if (buildSystem.IsRunningOnGitHubActions)
        {
            repositoryBranch = buildSystem.GitHubActions.Environment.Workflow.Ref.Replace("refs/heads/", "");
        }

        context.Information("Repository Branch: {0}", repositoryBranch);

        return !string.IsNullOrWhiteSpace(repositoryBranch) && StringComparer.OrdinalIgnoreCase.Equals("main", repositoryBranch);
    }

    public static bool IsTagged(this ICakeContext context)
    {
        var sha = context.ExecGitCmd("rev-parse --verify HEAD").Single();
        var isTagged = context.ExecGitCmd("tag --points-at " + sha).Any();

        return isTagged;
    }

    public static void ValidateOutput(this ICakeContext context, string cmd, string? args, string? expected)
    {
        var output = context.ExecuteCommand(cmd, args);
        var outputStr = string.Concat(output);
        context.Information(outputStr);

        Assert.Equal(expected, outputStr);
    }

    public static bool IsEnabled(this ICakeContext context, string envVar, bool nullOrEmptyAsEnabled = true)
    {
        var value = context.EnvironmentVariable(envVar);

        return string.IsNullOrWhiteSpace(value) ? nullOrEmptyAsEnabled : bool.Parse(value);
    }

    public static string GetOS(this ICakeContext context)
    {
        if (context.IsRunningOnWindows()) return "Windows";
        if (context.IsRunningOnLinux()) return "Linux";
        if (context.IsRunningOnMacOs()) return "macOs";
        return string.Empty;
    }

    public static string GetBuildAgent(this ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        return buildSystem.Provider switch
        {
            BuildProvider.Local => "Local",
            BuildProvider.AppVeyor => "AppVeyor",
            BuildProvider.AzurePipelines => "AzurePipelines",
            BuildProvider.AzurePipelinesHosted => "AzurePipelines",
            BuildProvider.GitHubActions => "GitHubActions",
            _ => string.Empty
        };
    }

    public static void StartGroup(this ICakeContext context, string title)
    {
        var buildSystem = context.BuildSystem();
        var startGroup = "[group]";
        if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted)
        {
            startGroup = "##[group]";
        }
        else if (buildSystem.IsRunningOnGitHubActions)
        {
            startGroup = "::group::";
        }
        context.Information($"{startGroup}{title}");
    }
    public static void EndGroup(this ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        var endgroup = "[endgroup]";
        if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted)
        {
            endgroup = "##[endgroup]";
        }
        else if (buildSystem.IsRunningOnGitHubActions)
        {
            endgroup = "::endgroup::";
        }
        context.Information($"{endgroup}");
    }

    public static bool ShouldRun(this ICakeContext context, bool criteria, string skipMessage)
    {
        if (criteria) return true;

        context.Information(skipMessage);
        return false;
    }
}
