using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitVersion.BuildAgents;
using GitVersion.Helpers;
using GitVersionCore.Tests.Helpers;

namespace GitVersionExe.Tests
{
    public static class GitVersionHelper
    {
        public static ExecutionResults ExecuteIn(string workingDirectory,
            string exec = null,
            string execArgs = null,
            string projectFile = null,
            string projectArgs = null,
            bool logToFile = true,
            params KeyValuePair<string, string>[] environments
            )
        {
            var logFile = logToFile ? Path.Combine(workingDirectory, "log.txt") : null;
            var args = new ArgumentBuilder(workingDirectory, exec, execArgs, projectFile, projectArgs, logFile);
            return ExecuteIn(args, environments);
        }

        public static ExecutionResults ExecuteIn(
            string workingDirectory,
            string arguments,
            bool logToFile = true,
            params KeyValuePair<string, string>[] environments)
        {
            var logFile = logToFile ? Path.Combine(workingDirectory, "log.txt") : null;
            var args = new ArgumentBuilder(workingDirectory, arguments, logFile);
            return ExecuteIn(args, environments);
        }

        private static ExecutionResults ExecuteIn(ArgumentBuilder arguments,
            params KeyValuePair<string, string>[] environments
        )
        {
            var executable = PathHelper.GetExecutable();
            var output = new StringBuilder();

            var environmentalVariables = new Dictionary<string, string>
            {
                { TeamCity.EnvironmentVariableName, null },
                { AppVeyor.EnvironmentVariableName, null },
                { TravisCi.EnvironmentVariableName, null },
                { Jenkins.EnvironmentVariableName, null },
                { AzurePipelines.EnvironmentVariableName, null },
                { GitHubActions.EnvironmentVariableName, null },
            };

            foreach (var environment in environments)
            {
                if (environmentalVariables.ContainsKey(environment.Key))
                {
                    environmentalVariables[environment.Key] = environment.Value;
                }
                else
                {
                    environmentalVariables.Add(environment.Key, environment.Value);
                }
            }

            var exitCode = -1;

            try
            {
                var args = PathHelper.GetExecutableArgs(arguments.ToString());

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

            if (string.IsNullOrWhiteSpace(arguments.LogFile) || !File.Exists(arguments.LogFile))
            {
                return new ExecutionResults(exitCode, output.ToString(), null);
            }

            var logContents = File.ReadAllText(arguments.LogFile);
            Console.WriteLine("Log from gitversion tool");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(logContents);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");

            return new ExecutionResults(exitCode, output.ToString(), logContents);
        }
    }
}
