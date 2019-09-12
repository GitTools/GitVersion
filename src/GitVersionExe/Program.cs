using GitVersion.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.OutputFormatters;
using GitVersion.Common;
using Environment = GitVersion.Common.Environment;

namespace GitVersion
{
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

            System.Environment.Exit(exitCode);
        }

        static int VerifyArgumentsAndRun()
        {
            Arguments arguments = null;
            try
            {
                var fileSystem = new FileSystem();
                var environment = new Environment();
                var argumentsWithoutExeName = GetArgumentsWithoutExeName();

                try
                {
                    var argumentParser = new ArgumentParser();
                    arguments = argumentParser.ParseArguments(argumentsWithoutExeName);
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

                if (arguments.IsVersion)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    VersionWriter.Write(assembly);
                    return 0;
                }

                if (arguments.IsHelp)
                {
                    HelpWriter.Write();
                    return 0;
                }

                if (arguments.Diag)
                {
                    arguments.NoCache = true;
                    arguments.Output = OutputType.BuildServer;
                }

                ConfigureLogging(arguments);
                if (arguments.Diag)
                {
                    Logger.WriteInfo("Dumping commit graph: ");
                    LibGitExtensions.DumpGraph(arguments.TargetPath, Logger.WriteInfo, 100);
                }
                if (!Directory.Exists(arguments.TargetPath))
                {
                    Logger.WriteWarning($"The working directory '{arguments.TargetPath}' does not exist.");
                }
                else
                {
                    Logger.WriteInfo("Working directory: " + arguments.TargetPath);
                }
                VerifyConfiguration(arguments, fileSystem);

                if (arguments.Init)
                {
                    ConfigurationProvider.Init(arguments.TargetPath, fileSystem, new ConsoleAdapter(), arguments.ConfigFileLocator);
                    return 0;
                }
                if (arguments.ShowConfig)
                {
                    Console.WriteLine(ConfigurationProvider.GetEffectiveConfigAsString(arguments.TargetPath, fileSystem, arguments.ConfigFileLocator));
                    return 0;
                }

                if (!string.IsNullOrEmpty(arguments.Proj) || !string.IsNullOrEmpty(arguments.Exec))
                {
                    arguments.Output = OutputType.BuildServer;
                }

                SpecifiedArgumentRunner.Run(arguments, fileSystem, environment);
            }
            catch (WarningException exception)
            {
                var error = $"An error occurred:\r\n{exception.Message}";
                Logger.WriteWarning(error);
                return 1;
            }
            catch (Exception exception)
            {
                var error = $"An unexpected error occurred:\r\n{exception}";
                Logger.WriteError(error);

                if (arguments != null)
                {
                    Logger.WriteInfo(string.Empty);
                    Logger.WriteInfo("Attempting to show the current git graph (please include in issue): ");
                    Logger.WriteInfo("Showing max of 100 commits");

                    try
                    {
                        LibGitExtensions.DumpGraph(arguments.TargetPath, Logger.WriteInfo, 100);
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

        static void VerifyConfiguration(Arguments arguments, IFileSystem fileSystem)
        {
            var gitPreparer = new GitPreparer(arguments.TargetUrl, arguments.DynamicRepositoryLocation, arguments.Authentication, arguments.NoFetch, arguments.TargetPath);
            arguments.ConfigFileLocator.Verify(gitPreparer, fileSystem);
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
                    logFile.Directory?.Create();

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
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= VerbosityLevel.Debug) a(s); }),
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= VerbosityLevel.Info) a(s); }),
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= VerbosityLevel.Warn) a(s); }),
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= VerbosityLevel.Error) a(s); }));

            if (exception != null)
                Logger.WriteError($"Failed to configure logging for '{arguments.LogFilePath}': {exception.Message}");
        }

        static void WriteLogEntry(Arguments arguments, string s)
        {
            var contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t\t{s}\r\n";
            File.AppendAllText(arguments.LogFilePath, contents);
        }

        static List<string> GetArgumentsWithoutExeName()
        {
            return System.Environment.GetCommandLineArgs()
                .Skip(1)
                .ToList();
        }
    }
}
