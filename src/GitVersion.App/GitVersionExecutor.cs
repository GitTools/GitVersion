using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion;

internal class GitVersionExecutor(
    ILogger<GitVersionExecutor> logger,
    IFileSystem fileSystem,
    IConsole console,
    IVersionWriter versionWriter,
    IHelpWriter helpWriter,
    IConfigurationFileLocator configurationFileLocator,
    IConfigurationProvider configurationProvider,
    IConfigurationSerializer configurationSerializer,
    IGitVersionCalculateTool gitVersionCalculateTool,
    IGitVersionOutputTool gitVersionOutputTool,
    IGitRepository gitRepository,
    IGitRepositoryInfo repositoryInfo)
    : IGitVersionExecutor
{
    private readonly ILogger<GitVersionExecutor> logger = logger.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IConsole console = console.NotNull();
    private readonly IVersionWriter versionWriter = versionWriter.NotNull();
    private readonly IHelpWriter helpWriter = helpWriter.NotNull();

    private readonly IConfigurationFileLocator configurationFileLocator = configurationFileLocator.NotNull();
    private readonly IConfigurationProvider configurationProvider = configurationProvider.NotNull();
    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();

    private readonly IGitVersionCalculateTool gitVersionCalculateTool = gitVersionCalculateTool.NotNull();
    private readonly IGitVersionOutputTool gitVersionOutputTool = gitVersionOutputTool.NotNull();
    private readonly IGitRepository gitRepository = gitRepository.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public int Execute(GitVersionOptions gitVersionOptions)
    {
        if (!HandleNonMainCommand(gitVersionOptions, out var exitCode))
        {
            exitCode = RunGitVersionTool(gitVersionOptions);
        }

        if (exitCode != 0)
        {
            // Inform user where to find detailed logs if a log file was configured
            var logFilePath = gitVersionOptions.LogFilePath;
            if (!logFilePath.IsNullOrWhiteSpace() && !logFilePath.Equals("console", StringComparison.OrdinalIgnoreCase))
            {
                this.console.WriteLine($"See log file for more details: {logFilePath}");
            }
        }

        return exitCode;
    }

    private int RunGitVersionTool(GitVersionOptions gitVersionOptions)
    {
        this.gitRepository.DiscoverRepository(gitVersionOptions.WorkingDirectory);
        var mutexName = this.repositoryInfo.DotGitDirectory?.Replace(FileSystemHelper.Path.DirectorySeparatorChar.ToString(), "") ?? string.Empty;
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
            this.logger.LogWarning("An error occurred: {Message}", exception.Message);
            this.console.WriteLine($"An error occurred:{FileSystemHelper.Path.NewLine}{exception.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "An unexpected error occurred");
            this.console.WriteLine($"An unexpected error occurred:{FileSystemHelper.Path.NewLine}{exception}");

            try
            {
                GitExtensions.DumpGraphLog(logMessage => this.logger.LogInformation("{LogMessage}", logMessage));
            }
            catch (Exception dumpGraphException)
            {
                this.logger.LogError(dumpGraphException, "Couldn't dump the git graph");
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
        }

        // Configure logging with the specified log file path
        // Console output is enabled for buildserver output mode or when -l console is specified
        var enableConsoleOutput = gitVersionOptions.Output.Contains(OutputType.BuildServer) ||
            string.Equals(gitVersionOptions.LogFilePath, "console", StringComparison.OrdinalIgnoreCase);
        LoggingEnricher.Configure(gitVersionOptions.LogFilePath, gitVersionOptions.Verbosity, enableConsoleOutput);

        var workingDirectory = gitVersionOptions.WorkingDirectory;
        if (gitVersionOptions.Diag)
        {
            GitExtensions.DumpGraphLog(logMessage => this.logger.LogInformation(logMessage));
        }

        if (!this.fileSystem.Directory.Exists(workingDirectory))
        {
            this.logger.LogWarning("The working directory '{WorkingDirectory}' does not exist.", workingDirectory);
        }
        else
        {
            this.logger.LogInformation("Working directory: {WorkingDirectory}", workingDirectory);
        }

        if (gitVersionOptions.ConfigurationInfo.ShowConfiguration)
        {
            if (gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
            {
                this.configurationFileLocator.Verify(workingDirectory, this.repositoryInfo.ProjectRootDirectory);
            }
            var configuration = this.configurationProvider.Provide();
            var configurationString = configurationSerializer.Serialize(configuration);
            this.console.WriteLine(configurationString);
            exitCode = 0;
            return true;
        }

        exitCode = 0;
        return false;
    }
}
