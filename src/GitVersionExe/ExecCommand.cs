using GitVersion.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GitVersion.Exceptions;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersion.Extensions;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class ExecCommand : IExecCommand
    {
        private static readonly bool RunningOnUnix = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private readonly IFileSystem fileSystem;
        private readonly IBuildServerResolver buildServerResolver;
        private readonly ILog log;
        private readonly IGitVersionCalculator gitVersionCalculator;
        private readonly IOptions<Arguments> options;
        public static readonly string BuildTool = GetMsBuildToolPath();

        public ExecCommand(IFileSystem fileSystem, IBuildServerResolver buildServerResolver, ILog log, IGitVersionCalculator gitVersionCalculator, IOptions<Arguments> options)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.buildServerResolver = buildServerResolver ?? throw new ArgumentNullException(nameof(buildServerResolver));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.gitVersionCalculator = gitVersionCalculator ?? throw new ArgumentNullException(nameof(gitVersionCalculator));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Execute()
        {
            log.Info($"Running on {(RunningOnUnix ? "Unix" : "Windows")}.");

            var variables = gitVersionCalculator.CalculateVersionVariables();

            var arguments = options.Value;

            switch (arguments.Output)
            {
                case OutputType.BuildServer:
                {
                    var buildServer = buildServerResolver.Resolve();
                    buildServer?.WriteIntegration(Console.WriteLine, variables);

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
                using var wixVersionFileUpdater = new WixVersionFileUpdater(arguments.TargetPath, variables, fileSystem, log);
                wixVersionFileUpdater.Update();
            }

            using var assemblyInfoUpdater = new AssemblyInfoFileUpdater(arguments.UpdateAssemblyInfoFileName, arguments.TargetPath, variables, fileSystem, log, arguments.EnsureAssemblyInfo);
            if (arguments.UpdateAssemblyInfo)
            {
                assemblyInfoUpdater.Update();
            }

            var execRun = RunExecCommandIfNeeded(arguments, arguments.TargetPath, variables, log);
            var msbuildRun = RunMsBuildIfNeeded(arguments, arguments.TargetPath, variables, log);

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
                return "/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild";
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
