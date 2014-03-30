using System;
using System.IO;
using System.Text;
using LibGit2Sharp;

namespace GitHubFlowVersion.AcceptanceTests.Helpers
{
    using System.Collections.Generic;
    using System.Web.Script.Serialization;

    public static class GitVersionHelper
    {
        public static ExecutionResults ExecuteIn(string workingDirectory, 
            string exec = null, string execArgs = null, string projectFile = null, string targets = null,
            bool isTeamCity = false)
        {
            var logFile = Path.Combine(workingDirectory, "log.txt");
            var gitHubFlowVersion = Path.Combine(PathHelper.GetCurrentDirectory(), "GitVersion.exe");
            var execArg = exec == null ? null : string.Format(" /Exec \"{0}\"", exec);
            var execArgsArg = execArgs == null ? null : string.Format(" /ExecArgs \"{0}\"", execArgs);
            var projectFileArg = projectFile == null ? null : string.Format(" /Proj \"{0}\"", projectFile);
            var targetsArg = targets == null ? null : string.Format(" /Targets \"{0}\"", targets);
            var logArg = string.Format(" /l \"{0}\"", logFile);
            var arguments = string.Format("\"{0}\"{1}{2}{3}{4}{5}", workingDirectory, execArg, execArgsArg,
                projectFileArg, targetsArg, logArg);

            var output = new StringBuilder();

            Console.WriteLine("Executing: {0} {1}", gitHubFlowVersion, arguments);
            Console.WriteLine();
            var environmentalVariables = isTeamCity ?
                new []{new KeyValuePair<string, string>("TEAMCITY_VERSION", "8.0.0") } :
                new KeyValuePair<string, string>[0];

            var exitCode = ProcessHelper.Run(
                s => output.AppendLine(s), s => output.AppendLine(s), null, 
                gitHubFlowVersion, arguments, workingDirectory,
                environmentalVariables);

            Console.WriteLine("Output from GitVersion.exe");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(output.ToString());
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine("Log from GitVersion.exe");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(File.ReadAllText(logFile));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");

            return new ExecutionResults(exitCode, new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(output.ToString()));
        }

        public static void AddNextVersionTxtFile(this IRepository repository, string version)
        {
            var nextVersionFile = Path.Combine(repository.Info.WorkingDirectory, "NextVersion.txt");
            File.WriteAllText(nextVersionFile, version);
        }
    }
}
