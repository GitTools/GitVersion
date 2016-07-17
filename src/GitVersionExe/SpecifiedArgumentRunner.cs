namespace GitVersion
{
    using GitTools;
    using GitVersion.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WarningException = System.ComponentModel.WarningException;

    class SpecifiedArgumentRunner
    {
        private static readonly bool runningOnMono = Type.GetType("Mono.Runtime") != null;
        public static readonly string BuildTool = runningOnMono ? "xbuild" : @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";

        public static void Run(Arguments arguments, IFileSystem fileSystem)
        {
            Logger.WriteInfo(string.Format("Running on {0}.", runningOnMono ? "Mono" : "Windows"));

            var noFetch = arguments.NoFetch;
            var authentication = arguments.Authentication;
            var targetPath = arguments.TargetPath;
            var targetUrl = arguments.TargetUrl;
            var dynamicRepositoryLocation = arguments.DynamicRepositoryLocation;
            var targetBranch = arguments.TargetBranch;
            var commitId = arguments.CommitId;
            var overrideConfig = arguments.HasOverrideConfig ? arguments.OverrideConfig : null;

            var executeCore = new ExecuteCore(fileSystem);
            var variables = executeCore.ExecuteGitVersion(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, targetPath, commitId, overrideConfig);

            if (arguments.Output == OutputType.BuildServer)
            {
                foreach (var buildServer in BuildServerList.GetApplicableBuildServers())
                {
                    buildServer.WriteIntegration(Console.WriteLine, variables);
                }
            }

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

            Logger.WriteInfo(string.Format("Launching build tool {0} \"{1}\" {2}", BuildTool, args.Proj, args.ProjArgs));
            var results = ProcessHelper.Run(
                Logger.WriteInfo, Logger.WriteError,
                null, BuildTool, string.Format("\"{0}\" {1}", args.Proj, args.ProjArgs), workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException(string.Format("{0} execution failed, non-zero return code", runningOnMono ? "XBuild" : "MSBuild"));

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