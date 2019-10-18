using GitVersion.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GitVersion.BuildServers;
using GitVersion.Exceptions;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersion.Extensions;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;

namespace GitVersion
{
    public class ExecCommand
    {
        private static readonly bool runningOnUnix = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly string BuildTool = GetMsBuildToolPath();

        public void Execute(Arguments arguments, IFileSystem fileSystem, IEnvironment environment, ILog log, IConfigFileLocator configFileLocator)
        {
            log.Info($"Running on {(runningOnUnix ? "Unix" : "Windows")}.");

            var noFetch = arguments.NoFetch;
            var authentication = arguments.Authentication;
            var targetPath = arguments.TargetPath;
            var targetUrl = arguments.TargetUrl;
            var dynamicRepositoryLocation = arguments.DynamicRepositoryLocation;
            var targetBranch = arguments.TargetBranch;
            var commitId = arguments.CommitId;
            var overrideConfig = arguments.HasOverrideConfig ? arguments.OverrideConfig : null;
            var noCache = arguments.NoCache;
            var noNormalize = arguments.NoNormalize;

            var executeCore = new ExecuteCore(fileSystem, environment, log, configFileLocator);
            var variables = executeCore.ExecuteGitVersion(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, targetPath, commitId, overrideConfig, noCache, noNormalize);

            switch (arguments.Output)
            {
                case OutputType.BuildServer:
                {
                    BuildServerList.Init(environment, log);
                    foreach (var buildServer in BuildServerList.GetApplicableBuildServers(log))
                    {
                        buildServer.WriteIntegration(Console.WriteLine, variables);
                    }

                    break;
                }
                case OutputType.Json:
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

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (arguments.UpdateWixVersionFile)
            {
                using (var wixVersionFileUpdater = new WixVersionFileUpdater(targetPath, variables, fileSystem, log))
                {
                    wixVersionFileUpdater.Update();
                }
            }

            using (var assemblyInfoUpdater = new AssemblyInfoFileUpdater(arguments.UpdateAssemblyInfoFileName, targetPath, variables, fileSystem, log, arguments.EnsureAssemblyInfo))
            {
                if (arguments.UpdateAssemblyInfo)
                {
                    assemblyInfoUpdater.Update();
                }

                var execRun = RunExecCommandIfNeeded(arguments, targetPath, variables, log);
                var msbuildRun = RunMsBuildIfNeeded(arguments, targetPath, variables, log);

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

        private static string GetMsBuildToolPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "/usr/bin/msbuild";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "/usr/local/bin/msbuild";
            }
            throw new Exception("MsBuild not found");
        }

        private static bool RunMsBuildIfNeeded(Arguments args, string workingDirectory, VersionVariables variables, ILog log)
        {
            if (string.IsNullOrEmpty(args.Proj)) return false;

            log.Info($"Launching build tool {BuildTool} \"{args.Proj}\" {args.ProjArgs}");
            var results = ProcessHelper.Run(
                m => log.Info(m), m => log.Error(m),
                null, BuildTool, $"\"{args.Proj}\" {args.ProjArgs}", workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException("MSBuild execution failed, non-zero return code");

            return true;
        }

        private static bool RunExecCommandIfNeeded(Arguments args, string workingDirectory, VersionVariables variables, ILog log)
        {
            if (string.IsNullOrEmpty(args.Exec)) return false;

            log.Info($"Launching {args.Exec} {args.ExecArgs}");
            var results = ProcessHelper.Run(
                m => log.Info(m), m => log.Error(m),
                null, args.Exec, args.ExecArgs, workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException($"Execution of {args.Exec} failed, non-zero return code");

            return true;
        }

        private static KeyValuePair<string, string>[] GetEnvironmentalVariables(VersionVariables variables)
        {
            return variables
                .Select(v => new KeyValuePair<string, string>("GitVersion_" + v.Key, v.Value))
                .ToArray();
        }
    }
}
