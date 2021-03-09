using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.IO;

namespace Common.Utilities
{
    public static class ContextExtensions
    {
        private static IEnumerable<string> ExecuteCommand(this ICakeContext context, FilePath exe, string args)
        {
            var processSettings = new ProcessSettings { Arguments = args, RedirectStandardOutput = true };
            context.StartProcess(exe, processSettings, out var redirectedOutput);
            return redirectedOutput.ToList();
        }

        private static IEnumerable<string> ExecGitCmd(this ICakeContext context, string cmd)
        {
            var gitExe = context.Tools.Resolve(context.IsRunningOnWindows() ? "git.exe" : "git");
            return context.ExecuteCommand(gitExe, cmd);
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
            context.Information("Repository Name: {0}", repositoryName);

            return !string.IsNullOrWhiteSpace(repositoryName) && StringComparer.OrdinalIgnoreCase.Equals("gittools/GitVersion", repositoryName);
        }

        public static bool IsMainBranch(this ICakeContext context)
        {
            var buildSystem = context.BuildSystem();
            string repositoryBranch = ExecGitCmd(context, "rev-parse --abbrev-ref HEAD").Single();
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
            var sha = ExecGitCmd(context, "rev-parse --verify HEAD").Single();
            var isTagged = ExecGitCmd(context, "tag --points-at " + sha).Any();

            return isTagged;
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
    }
}
