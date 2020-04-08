using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class ExecCommand : IExecCommand
    {
        private readonly ILog log;
        private readonly IOptions<GitVersionOptions> options;

        public ExecCommand(ILog log, IOptions<GitVersionOptions> options)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Execute(VersionVariables variables)
        {
            var gitVersionOptions = options.Value;

            RunExecCommandIfNeeded(gitVersionOptions, gitVersionOptions.WorkingDirectory, variables, log);
            RunMsBuildIfNeeded(gitVersionOptions, gitVersionOptions.WorkingDirectory, variables, log);
        }

        private static bool RunMsBuildIfNeeded(GitVersionOptions args, string workingDirectory, VersionVariables variables, ILog log)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (string.IsNullOrEmpty(args.Proj)) return false;

            args.Exec = "dotnet";
            args.ExecArgs = $"msbuild \"{args.Proj}\" {args.ProjArgs}";
#pragma warning restore CS0612 // Type or member is obsolete

            return RunExecCommandIfNeeded(args, workingDirectory, variables, log);
        }

        private static bool RunExecCommandIfNeeded(GitVersionOptions args, string workingDirectory, VersionVariables variables, ILog log)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (string.IsNullOrEmpty(args.Exec)) return false;

            log.Info($"Launching {args.Exec} {args.ExecArgs}");
            var results = ProcessHelper.Run(
                m => log.Info(m), m => log.Error(m),
                null, args.Exec, args.ExecArgs, workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException($"Execution of {args.Exec} failed, non-zero return code");
#pragma warning restore CS0612 // Type or member is obsolete

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
