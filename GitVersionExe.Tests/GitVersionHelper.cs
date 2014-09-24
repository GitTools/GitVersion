namespace AcceptanceTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using GitVersion;
    using LibGit2Sharp;

    public static class GitVersionHelper
    {
        public static ExecutionResults ExecuteIn(string workingDirectory,
            string exec = null, string execArgs = null, string projectFile = null, string projectArgs = null,
            bool isTeamCity = false)
        {
            var logFile = Path.Combine(workingDirectory, "log.txt");
            var gitHubFlowVersion = Path.Combine(PathHelper.GetCurrentDirectory(), "GitVersion.exe");
            var execArg = exec == null ? null : string.Format(" /exec \"{0}\"", exec);
            var execArgsArg = execArgs == null ? null : string.Format(" /execArgs \"{0}\"", execArgs);
            var projectFileArg = projectFile == null ? null : string.Format(" /proj \"{0}\"", projectFile);
            var targetsArg = projectArgs == null ? null : string.Format(" /projargs \"{0}\"", projectArgs);
            var logArg = string.Format(" /l \"{0}\"", logFile);
            var arguments = string.Format("\"{0}\"{1}{2}{3}{4}{5}", workingDirectory, execArg, execArgsArg,
                projectFileArg, targetsArg, logArg);

            var output = new StringBuilder();

            Console.WriteLine("Executing: {0} {1}", gitHubFlowVersion, arguments);
            Console.WriteLine();
            var environmentalVariables =
                new[] { new KeyValuePair<string, string>("TEAMCITY_VERSION", isTeamCity ? "8.0.0" : null) };

            var exitCode = ProcessHelper.Run(
                s => output.AppendLine(s), s => output.AppendLine(s), null,
                gitHubFlowVersion, arguments, workingDirectory,
                environmentalVariables);

            var logContents = File.ReadAllText(logFile);
            Console.WriteLine("Output from GitVersion.exe");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(output.ToString());
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");
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
}
