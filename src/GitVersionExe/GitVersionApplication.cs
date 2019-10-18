using System;
using System.IO;
using System.Reflection;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputFormatters;

namespace GitVersion
{
    public class GitVersionApplication : IGitVersionApplication
    {
        private readonly IFileSystem fileSystem;
        private readonly IEnvironment environment;
        private readonly ILog log;
        private readonly IConfigFileLocator configFileLocator;
        private readonly IHelpWriter helpWriter;
        private readonly IVersionWriter versionWriter;

        public GitVersionApplication(IFileSystem fileSystem, IEnvironment environment, ILog log, IConfigFileLocator configFileLocator)
        {
            this.fileSystem = fileSystem;
            this.environment = environment;
            this.log = log;
            this.configFileLocator = configFileLocator;

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

                VerifyConfiguration(arguments);

                if (arguments.Init)
                {
                    ConfigurationProvider.Init(arguments.TargetPath, fileSystem, new ConsoleAdapter(), log, configFileLocator);
                    return 0;
                }
                if (arguments.ShowConfig)
                {
                    Console.WriteLine(ConfigurationProvider.GetEffectiveConfigAsString(arguments.TargetPath, configFileLocator));
                    return 0;
                }

                var execCommand = new ExecCommand();

                execCommand.Execute(arguments, fileSystem, environment, log, configFileLocator);
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

        private void VerifyConfiguration(Arguments arguments)
        {
            var gitPreparer = new GitPreparer(log, arguments);
            configFileLocator.Verify(gitPreparer);
        }

        private static void ConfigureLogging(Arguments arguments, ILog log)
        {
            if (arguments.Output == OutputType.BuildServer || arguments.LogFilePath == "console" || arguments.Init)
            {
                log.AddLogAppender(new ConsoleAppender());
            }

            if (arguments.LogFilePath != null && arguments.LogFilePath != "console")
            {
                log.AddLogAppender(new FileAppender(arguments.LogFilePath));
            }
        }
    }
}
