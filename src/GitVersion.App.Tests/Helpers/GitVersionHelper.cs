using GitVersion.Agents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.App.Tests;

public static class GitVersionHelper
{
    public static ExecutionResults ExecuteIn(string workingDirectory,
        string? exec = null,
        string? execArgs = null,
        string? projectFile = null,
        string? projectArgs = null,
        bool logToFile = true,
        params KeyValuePair<string, string?>[] environments
    )
    {
        var logFile = logToFile ? PathHelper.Combine(workingDirectory, "log.txt") : null;
        var args = new ArgumentBuilder(workingDirectory, exec, execArgs, projectFile, projectArgs, logFile);
        return ExecuteIn(args, environments);
    }

    public static ExecutionResults ExecuteIn(
        string workingDirectory,
        string? arguments,
        bool logToFile = true,
        params KeyValuePair<string, string?>[] environments)
    {
        var logFile = logToFile ? PathHelper.Combine(workingDirectory, "log.txt") : null;
        var args = new ArgumentBuilder(workingDirectory, arguments, logFile);
        return ExecuteIn(args, environments);
    }

    private static ExecutionResults ExecuteIn(ArgumentBuilder arguments,
        params KeyValuePair<string, string?>[] environments
    )
    {
        var executable = ExecutableHelper.GetDotNetExecutable();
        var output = new StringBuilder();

        var environmentalVariables = new Dictionary<string, string?>
        {
            { TeamCity.EnvironmentVariableName, null },
            { AppVeyor.EnvironmentVariableName, null },
            { TravisCi.EnvironmentVariableName, null },
            { Jenkins.EnvironmentVariableName, null },
            { AzurePipelines.EnvironmentVariableName, null },
            { GitHubActions.EnvironmentVariableName, null },
            { SpaceAutomation.EnvironmentVariableName, null }
        };

        foreach (var (key, value) in environments)
        {
            environmentalVariables[key] = value;
        }

        var exitCode = -1;

        try
        {
            var args = ExecutableHelper.GetExecutableArgs(arguments.ToString());

            Console.WriteLine("Executing: {0} {1}", executable, args);
            Console.WriteLine();

            exitCode = ProcessHelper.Run(
                s => output.AppendLine(s),
                s => output.AppendLine(s),
                null,
                executable,
                args,
                arguments.WorkingDirectory,
                environmentalVariables.ToArray());
        }
        catch (Exception exception)
        {
            // NOTE: It's the exit code and output from the process we want to test,
            //       not the internals of the ProcessHelper. That's why we're catching
            //       any exceptions here, because problems in the process being executed
            //       should be visible in the output or exit code. @asbjornu
            Console.WriteLine(exception);
        }

        Console.WriteLine("Output from gitversion tool");
        Console.WriteLine("-------------------------------------------------------");
        Console.WriteLine(output.ToString());
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("-------------------------------------------------------");

        if (arguments.LogFile.IsNullOrWhiteSpace() || !File.Exists(arguments.LogFile))
        {
            return new(exitCode, output.ToString());
        }

        var logContents = File.ReadAllText(arguments.LogFile);
        Console.WriteLine("Log from gitversion tool");
        Console.WriteLine("-------------------------------------------------------");
        Console.WriteLine(logContents);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("-------------------------------------------------------");

        return new(exitCode, output.ToString(), logContents);
    }
}
