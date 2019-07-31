namespace GitVersion
{
    using GitVersion.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class SpecifiedArgumentRunner
    {
        private static readonly bool runningOnMono = Type.GetType("Mono.Runtime") != null;
        public static readonly string BuildTool = runningOnMono ? "xbuild" : @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";

        public static void Run(Arguments arguments, IFileSystem fileSystem)
        {
            Logger.WriteInfo($"Running on {(runningOnMono ? "Mono" : "Windows")}.");

            var noFetch = arguments.NoFetch;
            var authentication = arguments.Authentication;
            var targetPath = arguments.TargetPath;
            var targetUrl = arguments.TargetUrl;
            var dynamicRepositoryLocation = arguments.DynamicRepositoryLocation;
            var targetBranch = arguments.TargetBranch;
            var commitId = arguments.CommitId;
            var overrideConfig = arguments.HasOverrideConfig ? arguments.OverrideConfig : null;
            var noCache = arguments.NoCache;

            var executeCore = new ExecuteCore(fileSystem, arguments.ConfigFileLocator);
            var variables = executeCore.ExecuteGitVersion(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, targetPath, commitId, overrideConfig, noCache);

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
                        if (!variables.TryGetValue(arguments.ShowVariable, out var part))
                        {
                            throw new WarningException($"'{arguments.ShowVariable}' variable does not exist");
                        }
                        Console.WriteLine(part);
                        break;
                }
            }

            if (arguments.UpdateWixVersionFile)
            {
                using (var wixVersionFileUpdater = new WixVersionFileUpdater(targetPath, variables, fileSystem))
                {
                    wixVersionFileUpdater.Update();
                }
            }

            using (var assemblyInfoUpdater = new AssemblyInfoFileUpdater(arguments.UpdateAssemblyInfoFileName, targetPath, variables, fileSystem, arguments.EnsureAssemblyInfo))
            {
                if (arguments.UpdateAssemblyInfo)
                {
                    assemblyInfoUpdater.Update();
                }
                var execRun = false;
                var msbuildRun = false;
#if NETDESKTOP
                execRun = RunExecCommandIfNeeded(arguments, targetPath, variables);
                msbuildRun = RunMsBuildIfNeeded(arguments, targetPath, variables);
#endif

                if (!execRun && !msbuildRun)
                {
                    assemblyInfoUpdater.CommitChanges();
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
#if NETDESKTOP
        static bool RunMsBuildIfNeeded(Arguments args, string workingDirectory, VersionVariables variables)
        {


            if (string.IsNullOrEmpty(args.Proj)) return false;

            Logger.WriteInfo($"Launching build tool {BuildTool} \"{args.Proj}\" {args.ProjArgs}");
            var results = ProcessHelper.Run(
                Logger.WriteInfo, Logger.WriteError,
                null, BuildTool, $"\"{args.Proj}\" {args.ProjArgs}", workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException($"{(runningOnMono ? "XBuild" : "MSBuild")} execution failed, non-zero return code");

            return true;
        }


        static bool RunExecCommandIfNeeded(Arguments args, string workingDirectory, VersionVariables variables)
        {
            if (string.IsNullOrEmpty(args.Exec)) return false;

            Logger.WriteInfo($"Launching {args.Exec} {args.ExecArgs}");
            var results = ProcessHelper.Run(
                Logger.WriteInfo, Logger.WriteError,
                null, args.Exec, args.ExecArgs, workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException($"Execution of {args.Exec} failed, non-zero return code");

            return true;
        }
#endif
        static KeyValuePair<string, string>[] GetEnvironmentalVariables(VersionVariables variables)
        {
            return variables
                .Select(v => new KeyValuePair<string, string>("GitVersion_" + v.Key, v.Value))
                .ToArray();
        }
    }
}
