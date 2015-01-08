using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitVersion.Helpers;
using LibGit2Sharp;

public static class GitVersionHelper
{
    public static ExecutionResults ExecuteIn(string workingDirectory,
        string exec = null, string execArgs = null, string projectFile = null, string projectArgs = null,
        bool isTeamCity = false)
    {
        var logFile = Path.Combine(workingDirectory, "log.txt");
        var args = new ArgumentBuilder(workingDirectory, exec, execArgs, projectFile, projectArgs, logFile, isTeamCity);
        return ExecuteIn(args);
    }

    public static ExecutionResults ExecuteIn(string workingDirectory, string arguments, bool isTeamCity = false)
    {
        var args = new ArgumentBuilder(workingDirectory, arguments, isTeamCity);
        return ExecuteIn(args);
    }

    static ExecutionResults ExecuteIn(ArgumentBuilder arguments)
    {
        var gitHubFlowVersion = Path.Combine(PathHelper.GetCurrentDirectory(), "GitVersion.exe");
        var output = new StringBuilder();

        Console.WriteLine("Executing: {0} {1}", gitHubFlowVersion, arguments);
        Console.WriteLine();
        var environmentalVariables =
            new[]
            {
                new KeyValuePair<string, string>("TEAMCITY_VERSION", arguments.IsTeamCity ? "8.0.0" : null)
            };

        var exitCode = ProcessHelper.Run(
            s => output.AppendLine(s), s => output.AppendLine(s), null,
            gitHubFlowVersion, arguments.ToString(), arguments.WorkingDirectory,
            environmentalVariables);

        Console.WriteLine("Output from GitVersion.exe");
        Console.WriteLine("-------------------------------------------------------");
        Console.WriteLine(output.ToString());
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("-------------------------------------------------------");

        if (string.IsNullOrWhiteSpace(arguments.LogFile))
        {
            return new ExecutionResults(exitCode, output.ToString(), null);
        }

        var logContents = File.ReadAllText(arguments.LogFile);
        Console.WriteLine("Log from GitVersion.exe");
        Console.WriteLine("-------------------------------------------------------");
        Console.WriteLine(logContents);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("-------------------------------------------------------");

        return new ExecutionResults(exitCode, output.ToString(), logContents);
    }

    public static void AddNextVersionTxtFile(this IRepository repository, string version)
    {
        var nextVersionFile = Path.Combine(repository.Info.WorkingDirectory, "NextVersion.txt");
        File.WriteAllText(nextVersionFile, version);
    }
}