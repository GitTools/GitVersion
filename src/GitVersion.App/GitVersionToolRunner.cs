using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;

namespace GitVersion;

internal sealed class GitVersionToolRunner(
    ILogger<GitVersionExecutor> logger,
    Lazy<IGitVersionConfiguration> configuration,
    IGitVersionCalculateTool gitVersionCalculateTool,
    IGitVersionOutputTool gitVersionOutputTool,
    IGitRepository gitRepository,
    IGitRepositoryInfo repositoryInfo)
{
    // Preserve the executor's logging category when moving its execution pipeline.
    private readonly ILogger<GitVersionExecutor> logger = logger.NotNull();
    private readonly Lazy<IGitVersionConfiguration> configuration = configuration.NotNull();
    private readonly IGitVersionCalculateTool gitVersionCalculateTool = gitVersionCalculateTool.NotNull();
    private readonly IGitVersionOutputTool gitVersionOutputTool = gitVersionOutputTool.NotNull();
    private readonly IGitRepository gitRepository = gitRepository.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public int Run()
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
}
