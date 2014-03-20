using System;
using System.IO;
using System.Text;
using LibGit2Sharp;

namespace GitHubFlowVersion.AcceptanceTests.Helpers
{
    public static class GitVersionHelper
    {
        public static ExecutionResults ExecuteIn(string workingDirectory, string toFile = null, 
            string exec = null, string execArgs = null, string projectFile = null, string targets = null)
        {
            var gitHubFlowVersion = Path.Combine(PathHelper.GetCurrentDirectory(), "GitVersion.exe");
            var toFileArg = toFile == null ? null : string.Format(" /ToFile \"{0}\"", toFile);
            var execArg = exec == null ? null : string.Format(" /Exec \"{0}\"", exec);
            var execArgsArg = execArgs == null ? null : string.Format(" /ExecArgs \"{0}\"", execArgs);
            var projectFileArg = projectFile == null ? null : string.Format(" /ProjectFile \"{0}\"", projectFile);
            var targetsArg = targets == null ? null : string.Format(" /Targets \"{0}\"", targets);
            var arguments = string.Format("\"{0}\"{1}{2}{3}{4}{5}", workingDirectory, toFileArg, execArg, execArgsArg,
                projectFileArg, targetsArg);

            var output = new StringBuilder();

            Console.WriteLine("Executing: {0} {1}", gitHubFlowVersion, arguments);
            Console.WriteLine();
            var exitCode = ProcessHelper.Run(s => output.AppendLine(s), s => output.AppendLine(s), null, gitHubFlowVersion, arguments, workingDirectory);

            Console.WriteLine("Output from GitHubFlowVersion.exe");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(output.ToString());
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");

            return new ExecutionResults(exitCode, output.ToString());
        }

        public static void AddNextVersionTxtFile(this IRepository repository, string version)
        {
            var nextVersionFile = Path.Combine(repository.Info.WorkingDirectory, "NextVersion.txt");
            File.WriteAllText(nextVersionFile, version);
        }
    }
}
