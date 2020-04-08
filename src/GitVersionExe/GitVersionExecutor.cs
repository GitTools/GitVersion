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
        private readonly IConsole console;
        private readonly IConfigFileLocator configFileLocator;
        private readonly IHelpWriter helpWriter;
        private readonly IExecCommand execCommand;
        private readonly IConfigProvider configProvider;
        private readonly IGitVersionTool gitVersionTool;
        private readonly IVersionWriter versionWriter;

        public GitVersionExecutor(ILog log, IConsole console,
            IConfigFileLocator configFileLocator, IConfigProvider configProvider, IGitVersionTool gitVersionTool,
            IVersionWriter versionWriter, IHelpWriter helpWriter, IExecCommand execCommand)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.configFileLocator = configFileLocator ?? throw new ArgumentNullException(nameof(configFileLocator));
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configFileLocator));

            this.gitVersionTool = gitVersionTool ?? throw new ArgumentNullException(nameof(gitVersionTool));

            this.versionWriter = versionWriter ?? throw new ArgumentNullException(nameof(versionWriter));
            this.helpWriter = helpWriter ?? throw new ArgumentNullException(nameof(helpWriter));

            this.execCommand = execCommand ?? throw new ArgumentNullException(nameof(execCommand));
        }

        public int Execute(GitVersionOptions gitVersionOptions)
        {
            if (!HandleNonMainCommand(gitVersionOptions, out var exitCode))
            {
                exitCode = RunGitVersionTool(gitVersionOptions);
            }

            if (exitCode != 0)
            {
                // Dump log to console if we fail to complete successfully
                console.Write(log.ToString());
            }

            return exitCode;
        }

        private int RunGitVersionTool(GitVersionOptions gitVersionOptions)
        {
            try
            {
                var variables = gitVersionTool.CalculateVersionVariables();

                gitVersionTool.OutputVariables(variables);
                gitVersionTool.UpdateAssemblyInfo(variables);
                gitVersionTool.UpdateWixVersionFile(variables);

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

                if (gitVersionOptions == null) return 1;

                log.Info("Attempting to show the current git graph (please include in issue): ");
                log.Info("Showing max of 100 commits");

                try
                {
                    LibGitExtensions.DumpGraph(gitVersionOptions.WorkingDirectory, mess => log.Info(mess), 100);
                }
                catch (Exception dumpGraphException)
                {
                    log.Error("Couldn't dump the git graph due to the following error: " + dumpGraphException);
                }
                return 1;
            }

            return 0;
        }

        private bool HandleNonMainCommand(GitVersionOptions gitVersionOptions, out int exitCode)
        {
            if (gitVersionOptions == null)
            {
                helpWriter.Write();
                exitCode = 1;
                return true;
            }


            if (gitVersionOptions.IsVersion)
            {
                var assembly = Assembly.GetExecutingAssembly();
                versionWriter.Write(assembly);
                exitCode = 0;
                return true;
            }

            if (gitVersionOptions.IsHelp)
            {
                helpWriter.Write();
                exitCode = 0;
                return true;
            }

            if (gitVersionOptions.Diag)
            {
                gitVersionOptions.Settings.NoCache = true;
                gitVersionOptions.Output.Add(OutputType.BuildServer);
            }

#pragma warning disable CS0612 // Type or member is obsolete
            if (!string.IsNullOrEmpty(gitVersionOptions.Proj) || !string.IsNullOrEmpty(gitVersionOptions.Exec))
#pragma warning restore CS0612 // Type or member is obsolete
            {
                gitVersionOptions.Output.Add(OutputType.BuildServer);
            }

            ConfigureLogging(gitVersionOptions, log);

            var workingDirectory = gitVersionOptions.WorkingDirectory;
            if (gitVersionOptions.Diag)
            {
                log.Info("Dumping commit graph: ");
                LibGitExtensions.DumpGraph(workingDirectory, mess => log.Info(mess), 100);
            }

            if (!Directory.Exists(workingDirectory))
            {
                log.Warning($"The working directory '{workingDirectory}' does not exist.");
            }
            else
            {
                log.Info("Working directory: " + workingDirectory);
            }

            configFileLocator.Verify(gitVersionOptions);

            if (gitVersionOptions.Init)
            {
                configProvider.Init(workingDirectory);
                exitCode = 0;
                return true;
            }

            if (gitVersionOptions.ConfigInfo.ShowConfig)
            {
                var config = configProvider.Provide(workingDirectory);
                console.WriteLine(config.ToString());
                exitCode = 0;
                return true;
            }

            exitCode = 0;
            return false;
        }

        private static void ConfigureLogging(GitVersionOptions gitVersionOptions, ILog log)
        {
            if (gitVersionOptions.Output.Contains(OutputType.BuildServer) || gitVersionOptions.LogFilePath == "console" || gitVersionOptions.Init)
            {
                log.AddLogAppender(new ConsoleAppender());
            }

            if (gitVersionOptions.LogFilePath != null && gitVersionOptions.LogFilePath != "console")
            {
                log.AddLogAppender(new FileAppender(gitVersionOptions.LogFilePath));
            }
        }
    }
}
