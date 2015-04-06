namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GitVersion.Helpers;

    class SpecifiedArgumentRunner
    {
        const string MsBuild = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";

        public static void Run(Arguments arguments, IFileSystem fileSystem)
        {
            var gitPreparer = new GitPreparer(arguments);
            gitPreparer.InitialiseDynamicRepositoryIfNeeded();
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();
            if (string.IsNullOrEmpty(dotGitDirectory))
            {
                throw new Exception(string.Format("Failed to prepare or find the .git directory in path '{0}'", arguments.TargetPath));
            }
            var applicableBuildServers = GetApplicableBuildServers(arguments.Authentication).ToList();

            foreach (var buildServer in applicableBuildServers)
            {
                buildServer.PerformPreProcessingSteps(dotGitDirectory, arguments.NoFetch);
            }
            VersionVariables variables;
            var versionFinder = new GitVersionFinder();
            var configuration = ConfigurationProvider.Provide(dotGitDirectory, fileSystem);

            using (var repo = RepositoryLoader.GetRepo(dotGitDirectory))
            {
                var gitVersionContext = new GitVersionContext(repo, configuration, commitId: arguments.CommitId);
                var semanticVersion = versionFinder.FindVersion(gitVersionContext);
                var config = gitVersionContext.Configuration;
                variables = VariableProvider.GetVariablesFor(semanticVersion, config.AssemblyVersioningScheme, config.VersioningMode, config.ContinuousDeploymentFallbackTag, gitVersionContext.IsCurrentCommitTagged);
            }

            if (arguments.Output == OutputType.BuildServer)
            {
                foreach (var buildServer in applicableBuildServers)
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

            using (var assemblyInfoUpdate = new AssemblyInfoFileUpdate(arguments, arguments.TargetPath, variables, fileSystem))
            {
                var execRun = RunExecCommandIfNeeded(arguments, arguments.TargetPath, variables);
                var msbuildRun = RunMsBuildIfNeeded(arguments, arguments.TargetPath, variables);
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

        static IEnumerable<IBuildServer> GetApplicableBuildServers(Authentication authentication)
        {
            return BuildServerList.GetApplicableBuildServers(authentication);
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