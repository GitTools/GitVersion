using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.IO;

public static class ContextExtensions
{
    public static bool IsBuildTagged(this ICakeContext context)
    {
        var sha = ExecGitCmd(context, "rev-parse --verify HEAD").Single();
        var isTagged = ExecGitCmd(context, "tag --points-at " + sha).Any();

        return isTagged;
    }

    public static IEnumerable<string> ExecGitCmd(this ICakeContext context, string cmd)
    {
        var gitExe = context.Tools.Resolve(context.IsRunningOnWindows() ? "git.exe" : "git");
        return context.ExecuteCommand(gitExe, cmd);
    }

    public static IEnumerable<string> ExecuteCommand(this ICakeContext context, FilePath exe, string args)
    {
        context.StartProcess(exe, new ProcessSettings { Arguments = args, RedirectStandardOutput = true },
            out var redirectedOutput);

        return redirectedOutput.ToList();
    }

    private static string? GetEnvironmentValueOrArgument(this ICakeContext context, string environmentVariable,
        string argumentName)
    {
        var arg = context.EnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(arg))
        {
            arg = context.Argument<string?>(argumentName, null);
        }

        return arg;
    }
    
    public static void LogGroup(this ICakeContext context, string title, Action action)
    {
        context.StartGroup(title);
        action();
        context.EndGroup();
    }
    public static T LogGroup<T>(this ICakeContext context, string title, Func<T> action)
    {
        context.StartGroup(title);
        var result = action();
        context.EndGroup();

        return result;
    }
    public static void StartGroup(this ICakeContext context, string title)
    {
        var buildSystem = context.BuildSystem();
        var startGroup = "[group]";
        if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted) {
            startGroup = "##[group]";
        } else if (buildSystem.IsRunningOnGitHubActions) {
            startGroup = "::group::";
        }
        context.Information($"{startGroup}{title}");
    }
    public static void EndGroup(this ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        var endGroup = "[endgroup]";
        if (buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted) {
            endGroup = "##[endgroup]";
        } else if (buildSystem.IsRunningOnGitHubActions) {
            endGroup = "::endgroup::";
        }
        context.Information($"{endGroup}");
    }
}