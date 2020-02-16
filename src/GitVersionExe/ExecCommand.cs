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
                    var buildServer = buildServerResolver.Resolve();
                    buildServer?.WriteIntegration(Console.WriteLine, variables);

                    break;
                case OutputType.Json:
                    switch (arguments.ShowVariable)
                    {
                        case null:
                            Console.WriteLine(variables.ToString());
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
                assemblyInfoUpdater.CommitChanges();
            }

            RunExecCommandIfNeeded(arguments, arguments.TargetPath, variables, log);
            RunMsBuildIfNeeded(arguments, arguments.TargetPath, variables, log);
        }
        
        private static bool RunMsBuildIfNeeded(Arguments args, string workingDirectory, VersionVariables variables, ILog log)
        {
            if (string.IsNullOrEmpty(args.Proj)) return false;

            args.Exec = "dotnet";
            args.ExecArgs = $"msbuild \"{args.Proj}\" {args.ProjArgs}";

            return RunExecCommandIfNeeded(args, workingDirectory, variables, log);
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
