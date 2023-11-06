using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion;

internal class GitVersionExecutor : IGitVersionExecutor
{
    private readonly ILog log;
    private readonly IConsole console;
    private readonly IConfigurationFileLocator configurationFileLocator;
    private readonly IConfigurationProvider configurationProvider;
    private readonly IGitVersionCalculateTool gitVersionCalculateTool;
    private readonly IGitVersionOutputTool gitVersionOutputTool;
    private readonly IVersionWriter versionWriter;
    private readonly IHelpWriter helpWriter;
    private readonly IGitRepositoryInfo repositoryInfo;

    public GitVersionExecutor(ILog log, IConsole console,
        IConfigurationFileLocator configurationFileLocator, IConfigurationProvider configurationProvider,
        IGitVersionCalculateTool gitVersionCalculateTool, IGitVersionOutputTool gitVersionOutputTool,
        IVersionWriter versionWriter, IHelpWriter helpWriter, IGitRepositoryInfo repositoryInfo)
    {
        this.log = log.NotNull();
        this.console = console.NotNull();
        this.configurationFileLocator = configurationFileLocator.NotNull();
        this.configurationProvider = configurationProvider.NotNull();

        this.gitVersionCalculateTool = gitVersionCalculateTool.NotNull();
        this.gitVersionOutputTool = gitVersionOutputTool.NotNull();

        this.versionWriter = versionWriter.NotNull();
        this.helpWriter = helpWriter.NotNull();
        this.repositoryInfo = repositoryInfo.NotNull();
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
            this.console.Write(this.log.ToString());
        }

        return exitCode;
    }

    private int RunGitVersionTool(GitVersionOptions gitVersionOptions)
    {
        var mutexName = this.repositoryInfo.DotGitDirectory?.Replace(Path.DirectorySeparatorChar.ToString(), "") ?? string.Empty;
        using var mutex = new Mutex(true, $@"Global\gitversion{mutexName}", out var acquired);

        try
        {
            if (!acquired)
            {
                mutex.WaitOne();
            }

            var variables = this.gitVersionCalculateTool.CalculateVersionVariables();

            var configuration = this.configurationProvider.Provide(gitVersionOptions.ConfigurationInfo.OverrideConfiguration);

            this.gitVersionOutputTool.OutputVariables(variables, configuration.UpdateBuildNumber);
            this.gitVersionOutputTool.UpdateAssemblyInfo(variables);
            this.gitVersionOutputTool.UpdateWixVersionFile(variables);
        }
        catch (WarningException exception)
        {
            var error = $"An error occurred:{PathHelper.NewLine}{exception.Message}";
            this.log.Warning(error);
            return 1;
        }
        catch (Exception exception)
        {
            var error = $"An unexpected error occurred:{PathHelper.NewLine}{exception}";
            this.log.Error(error);

            this.log.Info("Attempting to show the current git graph (please include in issue): ");
            this.log.Info("Showing max of 100 commits");

            try
            {
                GitExtensions.DumpGraph(gitVersionOptions.WorkingDirectory, mess => this.log.Info(mess), 100);
            }
            catch (Exception dumpGraphException)
            {
                this.log.Error("Couldn't dump the git graph due to the following error: " + dumpGraphException);
            }
            return 1;
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        return 0;
    }

    private bool HandleNonMainCommand(GitVersionOptions gitVersionOptions, out int exitCode)
    {
        if (gitVersionOptions.IsVersion)
        {
            var assembly = Assembly.GetExecutingAssembly();
            this.versionWriter.Write(assembly);
            exitCode = 0;
            return true;
        }

        if (gitVersionOptions.IsHelp)
        {
            this.helpWriter.Write();
            exitCode = 0;
            return true;
        }

        if (gitVersionOptions.Diag)
        {
            gitVersionOptions.Settings.NoCache = true;
            gitVersionOptions.Output.Add(OutputType.BuildServer);
        }

        ConfigureLogging(gitVersionOptions, this.log);

        var workingDirectory = gitVersionOptions.WorkingDirectory;
        if (gitVersionOptions.Diag)
        {
            this.log.Info("Dumping commit graph: ");
            GitExtensions.DumpGraph(workingDirectory, mess => this.log.Info(mess), 100);
        }

        if (!Directory.Exists(workingDirectory))
        {
            this.log.Warning($"The working directory '{workingDirectory}' does not exist.");
        }
        else
        {
            this.log.Info("Working directory: " + workingDirectory);
        }

        if (gitVersionOptions.Init)
        {
            this.configurationProvider.Init(workingDirectory);
            exitCode = 0;
            return true;
        }

        if (gitVersionOptions.ConfigurationInfo.ShowConfiguration)
        {
            if (gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
            {
                this.configurationFileLocator.Verify(workingDirectory, this.repositoryInfo.ProjectRootDirectory);
            }
            var configuration = this.configurationProvider.Provide();
            this.console.WriteLine(configuration.ToJsonString());
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
