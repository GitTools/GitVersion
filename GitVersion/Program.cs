namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    class Program
    {
        private const string MsBuild = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";

        static void Main()
        {
            int? exitCode = null;

            try
            {
                var arguments = ArgumentParser.ParseArguments(GetArgumentsWithoutExeName());
                if (arguments.IsHelp)
                {
                    HelpWriter.Write();
                    return;
                }

                ConfigureLogging(arguments);

                var gitPreparer = new GitPreparer(arguments);
                var gitDirectory = gitPreparer.Prepare();
                if (string.IsNullOrEmpty(gitDirectory))
                {
                    Console.Error.WriteLine("Failed to prepare or find the .git directory in path '{0}'", arguments.TargetPath);
                    Environment.Exit(1);
                }

                var applicableBuildServers = GetApplicableBuildServers(arguments).ToList();

                foreach (var buildServer in applicableBuildServers)
                {
                    buildServer.PerformPreProcessingSteps(gitDirectory);
                }

                var semanticVersion = VersionCache.GetVersion(gitDirectory);

                if (arguments.Output == OutputType.BuildServer)
                {
                    foreach (var buildServer in applicableBuildServers)
                    {
                        buildServer.WriteIntegration(semanticVersion, Console.WriteLine);
                    }
                }

                if (arguments.Output == OutputType.Json)
                {
                    var variables = VariableProvider.GetVariablesFor(semanticVersion);
                    switch (arguments.VersionPart)
                    {
                        case null:
                            Console.WriteLine(JsonOutputFormatter.ToJson(variables));
                            break;

                        default:
                            string part;
                            if (!variables.TryGetValue(arguments.VersionPart, out part))
                            {
                                throw new ErrorException(string.Format("Could not extract '{0}' from the available parts.", arguments.VersionPart));
                            }
                            Console.WriteLine(part);
                            break;
                    }
                }

                RunMsBuildIfNeeded(arguments, gitDirectory);
                RunExecCommandIfNeeded(arguments, gitDirectory);

                if (gitPreparer.IsDynamicGitRepository)
                {
                    DeleteHelper.DeleteGitRepository(gitPreparer.DynamicGitRepositoryPath);
                }
            }
            catch (ErrorException exception)
            {
                var error = string.Format("An error occurred:\r\n{0}", exception.Message);
                Logger.WriteError(error);

                exitCode = 1;
            }
            catch (Exception exception)
            {
                var error = string.Format("An unexpected error occurred:\r\n{0}", exception);
                Logger.WriteWarning(error);

                exitCode = 1;
            }

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            if (!exitCode.HasValue)
            {
                exitCode = 0;
            }

            Environment.Exit(exitCode.Value);
        }

        static IEnumerable<IBuildServer> GetApplicableBuildServers(Arguments arguments)
        {
            return BuildServerList.GetApplicableBuildServers(arguments);
        }

        static void ConfigureLogging(Arguments arguments)
        {
            Action<string> writeAction = x => { };

            if (arguments.LogFilePath != null)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(arguments.LogFilePath));
                    if (File.Exists(arguments.LogFilePath))
                    {
                        using (File.CreateText(arguments.LogFilePath)) { }
                    }

                    writeAction = x =>
                    {
                        WriteLogEntry(arguments, x);
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to configure logging: " + ex.Message);
                }
            }

            Logger.WriteInfo = writeAction;
            Logger.WriteWarning = writeAction;
            Logger.WriteError = writeAction;
        }

        static void WriteLogEntry(Arguments arguments, string s)
        {
            var contents = string.Format("{0}\t\t{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), s);
            File.WriteAllText(arguments.LogFilePath, contents);
        }

        static List<string> GetArgumentsWithoutExeName()
        {
            return Environment.GetCommandLineArgs()
                .Skip(1)
                .ToList();
        }

        private static void RunMsBuildIfNeeded(Arguments args, string gitDirectory)
        {
            if (string.IsNullOrEmpty(args.Proj)) return;

            Console.WriteLine("Launching {0} \"{1}\"{2}", MsBuild, args.Proj, args.ProjArgs);
            var results = ProcessHelper.Run(
                Console.WriteLine, Console.Error.WriteLine,
                null, MsBuild, string.Format("\"{0}\"{1}", args.Proj, args.ProjArgs), gitDirectory);

            if (results != 0)
                throw new ErrorException("MsBuild execution failed, non-zero return code");
        }

        private static void RunExecCommandIfNeeded(Arguments args, string gitDirectory)
        {
            if (string.IsNullOrEmpty(args.Exec)) return;

            Console.WriteLine("Launching {0} {1}", args.Exec, args.ExecArgs);
            var results = ProcessHelper.Run(
                Console.WriteLine, Console.Error.WriteLine,
                null, args.Exec, args.ExecArgs, gitDirectory);
            if (results != 0)
                throw new ErrorException(string.Format("Execution of {0} failed, non-zero return code", args.Exec));
        }
    }
}