using System;
using System.IO;
using System.Reflection;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model;

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
        private readonly IGitVersionCalculator gitVersionCalculator;
        private readonly IVersionWriter versionWriter;

        public GitVersionExecutor(ILog log, IConfigFileLocator configFileLocator, IConfigProvider configProvider,
            IBuildServerResolver buildServerResolver, IGitPreparer gitPreparer, IGitVersionCalculator gitVersionCalculator,
            IVersionWriter versionWriter, IHelpWriter helpWriter, IExecCommand execCommand)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.configFileLocator = configFileLocator ?? throw new ArgumentNullException(nameof(configFileLocator));
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configFileLocator));

            this.buildServerResolver = buildServerResolver ?? throw new ArgumentNullException(nameof(buildServerResolver));
            this.gitPreparer = gitPreparer ?? throw new ArgumentNullException(nameof(gitPreparer));
            this.gitVersionCalculator = gitVersionCalculator ?? throw new ArgumentNullException(nameof(gitVersionCalculator));

            this.versionWriter = versionWriter ?? throw new ArgumentNullException(nameof(versionWriter));
            this.helpWriter = helpWriter ?? throw new ArgumentNullException(nameof(helpWriter));
            this.execCommand = execCommand ?? throw new ArgumentNullException(nameof(execCommand));
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
                if (HandleNonMainCommand(arguments, out var exitCode)) return exitCode;

                gitPreparer.Prepare();
                var variables = gitVersionCalculator.CalculateVersionVariables();
                execCommand.Execute(variables);
            }
            catch (WarningException exception)
            {
                var error = $"An error occurred:{System.Environment.NewLine}{exception.Message}";
                log.Warning(error);
                return 1;
            }
            catch (Exception exception)
            {
                var error = $"An unexpected error occurred:{System.Environment.NewLine}{exception}";
                log.Error(error);

                if (arguments == null) return 1;

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

        private bool HandleNonMainCommand(Arguments arguments, out int exitCode)
        {
            if (arguments == null)
            {
                helpWriter.Write();
                exitCode = 1;
                return true;
            }

            var targetPath = arguments.TargetPath;

            if (arguments.IsVersion)
            {
                var assembly = Assembly.GetExecutingAssembly();
                versionWriter.Write(assembly);
                exitCode = 0;
                return true;
            }

            if (arguments.IsHelp)
            {
                helpWriter.Write();
                exitCode = 0;
                return true;
            }

            if (arguments.Diag)
            {
                arguments.NoCache = true;
                arguments.Output.Add(OutputType.BuildServer);
            }

#pragma warning disable CS0612 // Type or member is obsolete
            if (!string.IsNullOrEmpty(arguments.Proj) || !string.IsNullOrEmpty(arguments.Exec))
#pragma warning restore CS0612 // Type or member is obsolete
            {
                arguments.Output.Add(OutputType.BuildServer);
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

            configFileLocator.Verify(arguments);

            if (arguments.Init)
            {
                configProvider.Init(targetPath);
                exitCode = 0;
                return true;
            }

            if (arguments.ShowConfig)
            {
                var config = configProvider.Provide(targetPath);
                Console.WriteLine(config.ToString());
                exitCode = 0;
                return true;
            }

            exitCode = 0;
            return false;
        }

        private static void ConfigureLogging(Arguments arguments, ILog log)
        {
            if (arguments.Output.Contains(OutputType.BuildServer) || arguments.LogFilePath == "console" || arguments.Init)
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
