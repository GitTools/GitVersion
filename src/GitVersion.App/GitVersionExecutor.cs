using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Git;
using Serilog;

namespace GitVersion;

internal class GitVersionExecutor(
    ILogger<GitVersionExecutor> logger,
    IFileSystem fileSystem,
    ConfigurationQueryExecutor configurationQueryExecutor,
    GitVersionToolRunner toolRunner)
    : IGitVersionExecutor
{
    private readonly ILogger<GitVersionExecutor> logger = logger.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly ConfigurationQueryExecutor configurationQueryExecutor = configurationQueryExecutor.NotNull();
    private readonly GitVersionToolRunner toolRunner = toolRunner.NotNull();

    public int Execute(GitVersionOptions gitVersionOptions)
    {
        Initialize(gitVersionOptions);

        var exitCode = !this.configurationQueryExecutor.TryExecute(gitVersionOptions)
            ? this.toolRunner.Run()
            : 0;

        if (exitCode != 0)
        {
            Log.CloseAndFlush();
        }

        return exitCode;
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
}
