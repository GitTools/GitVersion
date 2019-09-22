using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.Helpers;
using GitVersion.Log;
using GitVersion.OutputFormatters;

namespace GitVersion
{
    public class GitVersionApplication : IGitVersionApplication
    {
        private readonly IFileSystem fileSystem;
        private readonly IEnvironment environment;
        private readonly ILog log;
        private IHelpWriter helpWriter;
        private IVersionWriter versionWriter;

        public GitVersionApplication(IFileSystem fileSystem, IEnvironment environment, ILog log)
        {
            this.fileSystem = fileSystem;
            this.environment = environment;
            this.log = log;

            versionWriter = new VersionWriter();
            helpWriter = new HelpWriter(versionWriter);
        }

        public int Run(Arguments arguments)
        {
            var exitCode = VerifyArgumentsAndRun(arguments);

            if (exitCode != 0)
            {
                // Dump log to console if we fail to complete successfully
                Console.Write(log.ToString());
            }

            return exitCode;
        }

        private int VerifyArgumentsAndRun(Arguments arguments)
        {
            try
            {
                if (arguments == null)
                {
                    helpWriter.Write();
                    return 1;
                }

                if (arguments.IsVersion)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    versionWriter.Write(assembly);
                    return 0;
                }

                if (arguments.IsHelp)
                {
                    helpWriter.Write();
                    return 0;
                }

                if (arguments.Diag)
                {
                    arguments.NoCache = true;
                    arguments.Output = OutputType.BuildServer;
                }

                if (!string.IsNullOrEmpty(arguments.Proj) || !string.IsNullOrEmpty(arguments.Exec))
                {
                    arguments.Output = OutputType.BuildServer;
                }

                ConfigureLogging(arguments, log);

                if (arguments.Diag)
                {
                    log.Info("Dumping commit graph: ");
                    LibGitExtensions.DumpGraph(arguments.TargetPath, mess => log.Info(mess), 100);
                }
                if (!Directory.Exists(arguments.TargetPath))
                {
                    log.Warning($"The working directory '{arguments.TargetPath}' does not exist.");
                }
                else
                {
                    log.Info("Working directory: " + arguments.TargetPath);
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

                

                var execCommand = new ExecCommand();

                execCommand.Execute(arguments, fileSystem, environment, log);
            }
            catch (WarningException exception)
            {
                var error = $"An error occurred:\r\n{exception.Message}";
                log.Warning(error);
                return 1;
            }
            catch (Exception exception)
            {
                var error = $"An unexpected error occurred:\r\n{exception}";
                log.Error(error);

                if (arguments == null) return 1;

                log.Info(string.Empty);
                log.Info("Attempting to show the current git graph (please include in issue): ");
                log.Info("Showing max of 100 commits");

                try
                {
                    LibGitExtensions.DumpGraph(arguments.TargetPath, mess => log.Info(mess), 100);
                }
                catch (Exception dumpGraphException)
                {
                    log.Error("Couldn't dump the git graph due to the following error: " + dumpGraphException);
                }
                return 1;
            }

            return 0;
        }

        private static void VerifyConfiguration(Arguments arguments, IFileSystem fileSystem)
        {
            var gitPreparer = new GitPreparer(arguments);
            arguments.ConfigFileLocator.Verify(gitPreparer, fileSystem);
        }

        private static void ConfigureLogging(Arguments arguments, ILog log)
        {
            var writeActions = new List<Action<string>>
            {
                s => log.Info(s)
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
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= Verbosity.Diagnostic) a(s); }),
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= Verbosity.Normal) a(s); }),
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= Verbosity.Minimal) a(s); }),
                s => writeActions.ForEach(a => { if (arguments.Verbosity >= Verbosity.Quiet) a(s); }));

            if (exception != null)
                log.Error($"Failed to configure logging for '{arguments.LogFilePath}': {exception.Message}");
        }

        private static void WriteLogEntry(Arguments arguments, string s)
        {
            var contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t\t{s}\r\n";
            File.AppendAllText(arguments.LogFilePath, contents);
        }
    }
}
