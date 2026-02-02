using System.Runtime.InteropServices;
using Cake.Common.Build.AzurePipelines;
using Xunit;
using ProcessArchitecture = System.Runtime.InteropServices.Architecture;

namespace Common.Utilities;

#pragma warning disable S1144
public static class ContextExtensions
{
    extension(ICakeContext context)
    {
        public IEnumerable<string> ExecuteCommand(FilePath exe, string? args, DirectoryPath? workDir = null)
        {
            var processSettings = new ProcessSettings { Arguments = args, RedirectStandardOutput = true };
            if (workDir is not null)
            {
                processSettings.WorkingDirectory = workDir;
            }

            context.StartProcess(exe, processSettings, out var redirectedOutput);
            return redirectedOutput.ToList();
        }

        private IEnumerable<string> ExecGitCmd(string? cmd, DirectoryPath? workDir = null)
        {
            var gitExe = context.Tools.Resolve(context.IsRunningOnWindows() ? "git.exe" : "git");
            return context.ExecuteCommand(gitExe, cmd, workDir);
        }

        public void GitPushBranch(DirectoryPath repositoryDirectoryPath, string username, string token, string branchName)
        {
            var pushUrl = $"https://{username}:{token}@github.com/{Constants.RepoOwner}/{Constants.Repository}";
            context.ExecGitCmd($"push {pushUrl} {branchName}", workDir: repositoryDirectoryPath);
        }

        public bool IsOriginalRepo()
        {
            var repositoryName = context.GetRepositoryName();
            return !string.IsNullOrWhiteSpace(repositoryName) && StringComparer.OrdinalIgnoreCase.Equals("gittools/GitVersion", repositoryName);
        }

        public bool IsMainBranch()
        {
            var repositoryBranch = context.GetBranchName();
            return !string.IsNullOrWhiteSpace(repositoryBranch) && StringComparer.OrdinalIgnoreCase.Equals(Constants.DefaultBranch, repositoryBranch);
        }

        public bool IsSupportBranch()
        {
            var repositoryBranch = context.GetBranchName();
            return !string.IsNullOrWhiteSpace(repositoryBranch) && repositoryBranch.StartsWith("support/", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsNextBranch()
        {
            var repositoryBranch = context.GetBranchName();
            return !string.IsNullOrWhiteSpace(repositoryBranch) && repositoryBranch.StartsWith("next/", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsTagged()
        {
            var sha = context.ExecGitCmd("rev-parse --verify HEAD").Single();
            var isTagged = context.ExecGitCmd("tag --points-at " + sha).Any();

            return isTagged;
        }

        public void ValidateOutput(string cmd, string? args, string? expected)
        {
            var output = context.ExecuteCommand(cmd, args);
            var outputStr = string.Concat(output);
            context.Information(outputStr);

            Assert.Equal(expected, outputStr);
        }

        public bool IsEnabled(string envVar, bool nullOrEmptyAsEnabled = true)
        {
            var value = context.EnvironmentVariable(envVar);

            return string.IsNullOrWhiteSpace(value) ? nullOrEmptyAsEnabled : bool.Parse(value);
        }

        public string GetOS()
        {
            if (context.IsRunningOnWindows()) return "Windows";
            if (context.IsRunningOnLinux()) return "Linux";
            if (context.IsRunningOnMacOs()) return "macOs";

            return string.Empty;
        }

        public bool IsRunningOnAmd64()
            => RuntimeInformation.ProcessArchitecture == ProcessArchitecture.X64;

        public bool IsRunningOnArm64() =>
            RuntimeInformation.ProcessArchitecture == ProcessArchitecture.Arm64;

        public string GetBuildAgent()
        {
            var buildSystem = context.BuildSystem();
            return buildSystem.Provider switch
            {
                BuildProvider.Local => "Local",
                BuildProvider.AppVeyor => "AppVeyor",
                BuildProvider.AzurePipelines => "AzurePipelines",
                BuildProvider.GitHubActions => "GitHubActions",
                _ => string.Empty
            };
        }

        public void StartGroup(string title)
        {
            var buildSystem = context.BuildSystem();
            if (buildSystem.IsRunningOnAzurePipelines)
            {
                context.AzurePipelines().Commands.StartGroup(context, title);
            }
            else if (buildSystem.IsRunningOnGitHubActions)
            {
                context.GitHubActions().Commands.StartGroup(title);
            }
        }

        public void EndGroup()
        {
            var buildSystem = context.BuildSystem();
            if (buildSystem.IsRunningOnAzurePipelines)
            {
                context.AzurePipelines().Commands.EndGroup(context);
            }
            else if (buildSystem.IsRunningOnGitHubActions)
            {
                context.GitHubActions().Commands.EndGroup();
            }
        }

        public bool ShouldRun(bool criteria, string skipMessage)
        {
            if (criteria) return true;

            context.Information(skipMessage);
            return false;
        }

        public string GetBranchName()
        {
            var buildSystem = context.BuildSystem();
            var repositoryBranch = context.ExecGitCmd("rev-parse --abbrev-ref HEAD").Single();
            if (buildSystem.IsRunningOnAppVeyor)
            {
                repositoryBranch = buildSystem.AppVeyor.Environment.Repository.Branch;
            }
            else if (buildSystem.IsRunningOnAzurePipelines)
            {
                repositoryBranch = buildSystem.AzurePipelines.Environment.Repository.SourceBranchName;
            }
            else if (buildSystem.IsRunningOnGitHubActions)
            {
                repositoryBranch = buildSystem.GitHubActions.Environment.Workflow.Ref.Replace("refs/heads/", "");
            }

            return repositoryBranch;
        }

        public string GetRepositoryName()
        {
            var buildSystem = context.BuildSystem();
            var repositoryName = string.Empty;
            if (buildSystem.IsRunningOnAppVeyor)
            {
                repositoryName = buildSystem.AppVeyor.Environment.Repository.Name;
            }
            else if (buildSystem.IsRunningOnAzurePipelines)
            {
                repositoryName = buildSystem.AzurePipelines.Environment.Repository.RepoName;
            }
            else if (buildSystem.IsRunningOnGitHubActions)
            {
                repositoryName = buildSystem.GitHubActions.Environment.Workflow.Repository;
            }

            return repositoryName;
        }
        public FilePath? GetGitVersionToolLocation() =>
            context.GetFiles($"src/GitVersion.App/bin/{Constants.DefaultConfiguration}/net{Constants.DotnetLtsLatest}/gitversion.dll").SingleOrDefault();

        public FilePath? GetGitVersionDotnetToolLocation() =>
            context.MakeAbsolute(Paths.Tools.Combine("gitversion").CombineWithFilePath("gitversion.dll"));

        public FilePath? GetSchemaDotnetToolLocation() =>
            context.MakeAbsolute(Paths.Tools.Combine("schema").CombineWithFilePath("schema.dll"));
    }

    extension(IAzurePipelinesCommands _)
    {
        private void StartGroup(ICakeContext context, string title) => context.Information("##[group]{0}", title);
        private void EndGroup(ICakeContext context) => context.Information("##[endgroup]");
    }
}
#pragma warning restore S1144
