namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    class Program
    {
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

                var versionAndBranch = VersionCache.GetVersion(gitDirectory);

                if (arguments.Output == OutputType.BuildServer)
                {
                    foreach (var buildServer in applicableBuildServers)
                    {
                        buildServer.WriteIntegration(versionAndBranch, Console.WriteLine);
                    }
                }

                if (arguments.Output == OutputType.Json)
                {
                    var versionAsKeyValue = versionAndBranch.ToKeyValue();
                    switch (arguments.VersionPart)
                    {
                        case null:
                            Console.WriteLine(JsonOutputFormatter.ToJson(versionAsKeyValue));
                            break;

                        default:
                            string part;
                            if (!versionAsKeyValue.TryGetValue(arguments.VersionPart, out part))
                            {
                                throw new ErrorException(string.Format("Could not extract '{0}' from the available parts.", arguments.VersionPart));
                            }
                            Console.WriteLine(part);
                            break;
                    }
                }

                if (gitPreparer.IsDynamicGitRepository)
                {
                    DeleteHelper.DeleteGitRepository(gitPreparer.DynamicGitRepositoryPath);
                }
            }
            catch (ErrorException exception)
            {
                var error = string.Format("An error occurred:\r\n{0}", exception.Message);
                Logger.WriteWarning(error);

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
            Action<string> writeAction;

            if (arguments.LogFilePath == null)
            {
                writeAction = x =>
                {
                };
            }
            else
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

            Logger.WriteInfo = writeAction;
            Logger.WriteWarning = writeAction;
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
    }
}