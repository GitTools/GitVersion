namespace GitVersion
{
    using GitVersion.Helpers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    class Program
    {
        static StringBuilder log = new StringBuilder();

        static void Main()
        {
            var exitCode = VerifyArgumentsAndRun();

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            if (exitCode != 0)
            {
                // Dump log to console if we fail to complete successfully
                Console.Write(log.ToString());
            }

            Environment.Exit(exitCode);
        }

        static int VerifyArgumentsAndRun()
        {
            Arguments arguments = null;
            try
            {
                var fileSystem = new FileSystem();
                var argumentsWithoutExeName = GetArgumentsWithoutExeName();

                try
                {
                    arguments = ArgumentParser.ParseArguments(argumentsWithoutExeName);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Failed to parse arguments: {0}", string.Join(" ", argumentsWithoutExeName));
                    if (!string.IsNullOrWhiteSpace(exception.Message))
                    {
                        Console.WriteLine();
                        Console.WriteLine(exception.Message);
                        Console.WriteLine();
                    }

                    HelpWriter.Write();
                    return 1;
                }

                if (arguments.IsHelp)
                {
                    HelpWriter.Write();
                    return 0;
                }

                ConfigureLogging(arguments);
                if (!Directory.Exists(arguments.TargetPath))
                {
                    Logger.WriteWarning(string.Format("The working directory '{0}' does not exist.", arguments.TargetPath));
                }
                else
                {
                    Logger.WriteInfo("Working directory: " + arguments.TargetPath);
                }
                VerifyConfiguration(arguments, fileSystem);

                if (arguments.Init)
                {
                    ConfigurationProvider.Init(arguments.TargetPath, fileSystem, new ConsoleAdapter());
                    return 0;
                }
                if (arguments.ShowConfig)
                {
                    Console.WriteLine(ConfigurationProvider.GetEffectiveConfigAsString(arguments.TargetPath, fileSystem));
                    return 0;
                }

                if (!string.IsNullOrEmpty(arguments.Proj) || !string.IsNullOrEmpty(arguments.Exec))
                {
                    arguments.Output = OutputType.BuildServer;
                }

                SpecifiedArgumentRunner.Run(arguments, fileSystem);
            }
            catch (WarningException exception)
            {
                var error = string.Format("An error occurred:\r\n{0}", exception.Message);
                Logger.WriteWarning(error);
                return 1;
            }
            catch (Exception exception)
            {
                var error = string.Format("An unexpected error occurred:\r\n{0}", exception);
                Logger.WriteError(error);

                if (arguments != null)
                {
                    Logger.WriteInfo(string.Empty);
                    Logger.WriteInfo("Attempting to show the current git graph (please include in issue): ");
                    Logger.WriteInfo("Showing max of 100 commits");

                    try
                    {
                        GitTools.LibGitExtensions.DumpGraph(arguments.TargetPath, Logger.WriteInfo, 100);
                    }
                    catch (Exception dumpGraphException)
                    {
                        Logger.WriteError("Couldn't dump the git graph due to the following error: " + dumpGraphException);
                    }
                }
                return 1;
            }

            return 0;
        }

        private static void VerifyConfiguration(Arguments arguments, IFileSystem fileSystem)
        {
            var gitPreparer = new GitPreparer(arguments.TargetUrl, arguments.DynamicRepositoryLocation, arguments.Authentication, arguments.NoFetch, arguments.TargetPath);
            ConfigurationProvider.Verify(gitPreparer, fileSystem);
        }

        static void ConfigureLogging(Arguments arguments)
        {
            var writeActions = new List<Action<string>>
            {
                s => log.AppendLine(s)
            };

            if (arguments.Output == OutputType.BuildServer || arguments.LogFilePath == "console" || arguments.Init)
            {
                writeActions.Add(Console.WriteLine);
            }

            Exception exception = null;
            if (arguments.LogFilePath != null && arguments.LogFilePath != "console")
            {
                try
                {
                    var logFileFullPath = Path.GetFullPath(arguments.LogFilePath);
                    var logFile = new FileInfo(logFileFullPath);

                    // NOTE: logFile.Directory will be null if the path is i.e. C:\logfile.log. @asbjornu
                    if (logFile.Directory != null)
                    {
                        logFile.Directory.Create();
                    }

                    using (logFile.CreateText())
                    {
                    }

                    writeActions.Add(x => WriteLogEntry(arguments, x));
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            Logger.SetLoggers(
                s => writeActions.ForEach(a => a(s)),
                s => writeActions.ForEach(a => a(s)),
                s => writeActions.ForEach(a => a(s)));

            if (exception != null)
                Logger.WriteError(string.Format("Failed to configure logging for '{0}': {1}", arguments.LogFilePath, exception.Message));
        }

        static void WriteLogEntry(Arguments arguments, string s)
        {
            var contents = string.Format("{0}\t\t{1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), s);
            File.AppendAllText(arguments.LogFilePath, contents);
        }

        static List<string> GetArgumentsWithoutExeName()
        {
            return Environment.GetCommandLineArgs()
                .Skip(1)
                .ToList();
        }
    }
}
