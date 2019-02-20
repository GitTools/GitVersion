using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitTools;
using GitVersion;

public static class GitVersionHelper
{
    public static ExecutionResults ExecuteIn(string workingDirectory,
                                             string exec = null,
                                             string execArgs = null,
                                             string projectFile = null,
                                             string projectArgs = null,
                                             bool isTeamCity = false,
                                             bool logToFile = true)
    {
        var logFile = logToFile ? Path.Combine(workingDirectory, "log.txt") : null;
        var args = new ArgumentBuilder(workingDirectory, exec, execArgs, projectFile, projectArgs, logFile, isTeamCity);
        return ExecuteIn(args);
    }

    public static ExecutionResults ExecuteIn(string workingDirectory, string arguments, bool isTeamCity = false, bool logToFile = true)
    {
        var logFile = logToFile ? Path.Combine(workingDirectory, "log.txt") : null;
        var args = new ArgumentBuilder(workingDirectory, arguments, isTeamCity, logFile);
        return ExecuteIn(args);
    }

    static ExecutionResults ExecuteIn(ArgumentBuilder arguments)
    {
        var gitVersion = Path.Combine(PathHelper.GetCurrentDirectory(), "GitVersion.exe");
        var output = new StringBuilder();

        Console.WriteLine("Executing: {0} {1}", gitVersion, arguments);
        Console.WriteLine();
        var environmentalVariables =
            new[]
            {
                new KeyValuePair<string, string>(TeamCity.EnvironmentVariableName, arguments.IsTeamCity ? "8.0.0" : null),
                new KeyValuePair<string, string>(AppVeyor.EnvironmentVariableName, null),
                new KeyValuePair<string, string>(TravisCI.EnvironmentVariableName, null),
            };

        var exitCode = -1;

        try
        {
            exitCode = ProcessHelper.Run(
                s => output.AppendLine(s), s => output.AppendLine(s), null,
                gitVersion, arguments.ToString(), arguments.WorkingDirectory,
                environmentalVariables);
        }
        catch (Exception exception)
        {
            // NOTE: It's the exit code and output from the process we want to test,
            //       not the internals of the ProcessHelper. That's why we're catching
            //       any exceptions here, because problems in the process being executed
            //       should be visible in the output or exit code. @asbjornu
            Console.WriteLine(exception);
        }

        Console.WriteLine("Output from GitVersion.exe");
        Console.WriteLine("-------------------------------------------------------");
        Console.WriteLine(output.ToString());
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("-------------------------------------------------------");

        if (string.IsNullOrWhiteSpace(arguments.LogFile) || !File.Exists(arguments.LogFile))
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
}