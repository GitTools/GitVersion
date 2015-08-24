namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GitVersion.Helpers;

    public class InputVariables
    {
        public InputVariables()
        {
            Authentication = new Authentication();
            NoFetch = true;
        }

        public string TargetUrl { get; set; }
        public string DynamicRepositoryLocation { get; set; }
        public Authentication Authentication { get; set; }
        public string TargetBranch { get; set; }
        public bool NoFetch { get; set; }
        public string TargetPath { get; set; }
        public string CommitId { get; set; }
    }

    class SpecifiedArgumentRunner
    {
        const string MsBuild = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";

        public static VersionVariables GetVariables(IFileSystem fileSystem, InputVariables inputVariables)
        {
            return GetVariables(fileSystem,
                inputVariables.TargetUrl,
                inputVariables.DynamicRepositoryLocation,
                inputVariables.Authentication,
                inputVariables.TargetBranch,
                inputVariables.NoFetch,
                inputVariables.TargetPath,
                inputVariables.CommitId);
        }

        private static VersionVariables GetVariables(IFileSystem fileSystem, 
            string targetUrl, 
            string dynamicRepositoryLocation, 
            Authentication authentication, 
            string targetBranch, 
            bool noFetch, 
            string targetPath, 
            string commitId)
        {
            return ExecuteCore.ExecuteGitVersion(fileSystem, targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, targetPath, commitId);
        }

        public static void Run(Arguments arguments, IFileSystem fileSystem)
        {
            var noFetch = arguments.NoFetch;
            var authentication = arguments.Authentication;
            var targetPath = arguments.TargetPath;
            var targetUrl = arguments.TargetUrl;
            var dynamicRepositoryLocation = arguments.DynamicRepositoryLocation;
            var targetBranch = arguments.TargetBranch;
            var commitId = arguments.CommitId;

            var variables = GetVariables(fileSystem, targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, targetPath, commitId);

            if (arguments.Output == OutputType.Json)
            {
                switch (arguments.ShowVariable)
                {
                    case null:
                        Console.WriteLine(JsonOutputFormatter.ToJson(variables));
                        break;

                    default:
                        string part;
                        if (!variables.TryGetValue(arguments.ShowVariable, out part))
                        {
                            throw new WarningException(string.Format("'{0}' variable does not exist", arguments.ShowVariable));
                        }
                        Console.WriteLine(part);
                        break;
                }
            }

            using (var assemblyInfoUpdate = new AssemblyInfoFileUpdate(arguments, targetPath, variables, fileSystem))
            {
                var execRun = RunExecCommandIfNeeded(arguments, targetPath, variables);
                var msbuildRun = RunMsBuildIfNeeded(arguments, targetPath, variables);
                if (!execRun && !msbuildRun)
                {
                    assemblyInfoUpdate.DoNotRestoreAssemblyInfo();
                    //TODO Put warning back
                    //if (!context.CurrentBuildServer.IsRunningInBuildAgent())
                    //{
                    //    Console.WriteLine("WARNING: Not running in build server and /ProjectFile or /Exec arguments not passed");
                    //    Console.WriteLine();
                    //    Console.WriteLine("Run GitVersion.exe /? for help");
                    //}
                }
            }
        }

        static bool RunMsBuildIfNeeded(Arguments args, string workingDirectory, VersionVariables variables)
        {
            if (string.IsNullOrEmpty(args.Proj)) return false;

            Logger.WriteInfo(string.Format("Launching {0} \"{1}\" {2}", MsBuild, args.Proj, args.ProjArgs));
            var results = ProcessHelper.Run(
                Logger.WriteInfo, Logger.WriteError,
                null, MsBuild, string.Format("\"{0}\" {1}", args.Proj, args.ProjArgs), workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException("MsBuild execution failed, non-zero return code");

            return true;
        }

        static bool RunExecCommandIfNeeded(Arguments args, string workingDirectory, VersionVariables variables)
        {
            if (string.IsNullOrEmpty(args.Exec)) return false;

            Logger.WriteInfo(string.Format("Launching {0} {1}", args.Exec, args.ExecArgs));
            var results = ProcessHelper.Run(
                Logger.WriteInfo, Logger.WriteError,
                null, args.Exec, args.ExecArgs, workingDirectory,
                GetEnvironmentalVariables(variables));
            if (results != 0)
                throw new WarningException(string.Format("Execution of {0} failed, non-zero return code", args.Exec));

            return true;
        }

        static KeyValuePair<string, string>[] GetEnvironmentalVariables(VersionVariables variables)
        {
            return variables
                .Select(v => new KeyValuePair<string, string>("GitVersion_" + v.Key, v.Value))
                .ToArray();
        }
    }
}