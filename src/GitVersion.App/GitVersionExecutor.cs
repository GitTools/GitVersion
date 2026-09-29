using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using Serilog;

namespace GitVersion;

internal class GitVersionExecutor(
    ILogger<GitVersionExecutor> logger,
    IFileSystem fileSystem,
    IConsole console,
    IConfigurationFileLocator configurationFileLocator,
    IConfigurationProvider configurationProvider,
    Lazy<IGitVersionConfiguration> configuration,
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

    private readonly IConfigurationFileLocator configurationFileLocator = configurationFileLocator.NotNull();
    private readonly IConfigurationProvider configurationProvider = configurationProvider.NotNull();
    private readonly Lazy<IGitVersionConfiguration> configuration = configuration.NotNull();
    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();

    private readonly IGitVersionCalculateTool gitVersionCalculateTool = gitVersionCalculateTool.NotNull();
    private readonly IGitVersionOutputTool gitVersionOutputTool = gitVersionOutputTool.NotNull();
    private readonly IGitRepository gitRepository = gitRepository.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public int Execute(GitVersionOptions gitVersionOptions)
    {
        Initialize(gitVersionOptions);

        var exitCode = !VerifyAndDisplayConfiguration(gitVersionOptions)
            ? RunGitVersionTool()
            : 0;

        if (exitCode != 0)
        {
            Log.CloseAndFlush();
        }

        return exitCode;
    }

    private int RunGitVersionTool()
    {
        this.gitRepository.DiscoverRepository(this.repositoryInfo.GitRootPath);
        var mutexName = this.repositoryInfo.DotGitDirectory?.Replace(FileSystemHelper.Path.DirectorySeparatorChar.ToString(), "") ?? string.Empty;
        using var mutex = new Mutex(true, $@"Global\gitversion{mutexName}", out var acquired);

        try
        {
            if (!acquired)
            {
                mutex.WaitOne();
            }

            var variables = this.gitVersionCalculateTool.CalculateVersionVariables();

            this.gitVersionOutputTool.OutputVariables(variables, this.configuration.Value.UpdateBuildNumber);
            this.gitVersionOutputTool.UpdateAssemblyInfo(variables);
            this.gitVersionOutputTool.UpdateWixVersionFile(variables);
        }
        catch (Exception exception) when (exception is WarningException or ConfigurationException)
        {
            WriteError(exception);
            this.logger.LogError(exception, """
                                            An error occurred:
                                            {Message}
                                            """, exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, """
                                            An unexpected error occurred:
                                            {ExceptionMessage}
                                            """, exception.Message);

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

    private static void WriteError(Exception exception)
    {
        Console.Error.WriteLine("An error occurred:");
        Console.Error.WriteLine(exception.Message);
    }

    private void Initialize(GitVersionOptions gitVersionOptions)
    {
        if (gitVersionOptions.Diag)
        {
            gitVersionOptions.Settings.NoCache = true;
        }

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
    }

    private bool VerifyAndDisplayConfiguration(GitVersionOptions gitVersionOptions)
    {
        var configurationInfo = gitVersionOptions.ConfigurationInfo;
        if (!configurationInfo.ShowConfiguration && configurationInfo.PropertyPath is null)
        {
            return false;
        }

        var version = ConfigurationVersionSelector.Resolve();
        if (configurationInfo.PropertyPath is not null && version != ConfigurationVersion.V7)
        {
            throw new ConfigurationException("Configuration property queries require v7 configuration. Set GITVERSION_CONFIGURATION_VERSION=v7.");
        }

        if (gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
        {
            this.configurationFileLocator.Verify(gitVersionOptions.WorkingDirectory, this.repositoryInfo.ProjectRootDirectory);
        }

        var configurationValue = this.configurationProvider.Provide(version == ConfigurationVersion.V7 ? configurationInfo.OverrideConfiguration : null);
        var serializedConfiguration = configurationInfo.PropertyPath is { } path
            ? this.configurationSerializer.SerializeProperty(configurationValue, path)
            : this.configurationSerializer.Serialize(configurationValue);
        this.console.WriteLine(serializedConfiguration);
        return true;
    }
}
