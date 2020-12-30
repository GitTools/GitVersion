using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Core;
using Cake.Core.IO;

namespace Build
{
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
            context.StartProcess(exe, new ProcessSettings { Arguments = args, RedirectStandardOutput = true }, out var redirectedOutput);

            return redirectedOutput.ToList();
        }
        
        private static string GetEnvironmentValueOrArgument(this ICakeContext context, string environmentVariable,
            string argumentName)
        {
            var arg = context.EnvironmentVariable(environmentVariable);
            if (string.IsNullOrWhiteSpace(arg))
            {
                arg = context.Argument<string>(argumentName, null);
            }

            return arg;
        }
    }
}