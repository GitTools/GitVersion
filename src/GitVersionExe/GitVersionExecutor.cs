using System;
using System.IO;
using System.Reflection;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.Logging;
using GitVersion.OutputFormatters;
using GitVersion.Extensions;

namespace GitVersion
{
    public class GitVersionExecutor : IGitVersionExecutor
    {
        private readonly ILog log;
        private readonly IConfigFileLocator configFileLocator;
        private readonly IHelpWriter helpWriter;
        private readonly IExecCommand execCommand;
        private readonly IConfigProvider configProvider;
        private readonly IBuildServerResolver buildServerResolver;
        private readonly IGitPreparer gitPreparer;
        private readonly IVersionWriter versionWriter;

        public GitVersionExecutor(ILog log, IConfigFileLocator configFileLocator, IVersionWriter versionWriter, IHelpWriter helpWriter,
            IExecCommand execCommand, IConfigProvider configProvider, IBuildServerResolver buildServerResolver, IGitPreparer gitPreparer)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.configFileLocator = configFileLocator ?? throw new ArgumentNullException(nameof(configFileLocator));
            this.versionWriter = versionWriter ?? throw new ArgumentNullException(nameof(versionWriter));
            this.helpWriter = helpWriter ?? throw new ArgumentNullException(nameof(helpWriter));
            this.execCommand = execCommand ?? throw new ArgumentNullException(nameof(execCommand));
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configFileLocator));
            this.buildServerResolver = buildServerResolver ?? throw new ArgumentNullException(nameof(buildServerResolver));
            this.gitPreparer = gitPreparer;
        }

        public int Execute(Arguments arguments)
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
                var targetPath = arguments.TargetPath;

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

                var buildServer = buildServerResolver.Resolve();
                arguments.NoFetch = arguments.NoFetch || buildServer != null && buildServer.PreventFetch();

                ConfigureLogging(arguments, log);

                if (arguments.Diag)
                {
                    log.Info("Dumping commit graph: ");
                    LibGitExtensions.DumpGraph(targetPath, mess => log.Info(mess), 100);
                }
                if (!Directory.Exists(targetPath))
                {
                    log.Warning($"The working directory '{targetPath}' does not exist.");
                }
                else
                {
                    log.Info("Working directory: " + targetPath);
                }

                VerifyConfiguration();

                if (arguments.Init)
                {
                    configProvider.Init(targetPath);
                    return 0;
                }
                if (arguments.ShowConfig)
                {
                    var config = configProvider.Provide(targetPath);
                    Console.WriteLine(config.ToString());
                    return 0;
                }

                execCommand.Execute();
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

        private void VerifyConfiguration()
        {
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
