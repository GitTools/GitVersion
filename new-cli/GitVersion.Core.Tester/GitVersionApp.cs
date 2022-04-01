using GitVersion.Git;
using GitVersion.Infrastructure;

namespace GitVersion;

public class GitVersionApp
{
    private readonly ILogger logger;
    private readonly IGitRepository repository;
    public GitVersionApp(ILogger logger, IGitRepository repository)
    {
        this.logger = logger;
        this.repository = repository;
    }

    public Task<int> RunAsync(string[] args)
    {
        repository.Discover(Directory.GetCurrentDirectory());
        var branches = repository.Branches.ToList();
        logger.LogInformation("Found {count} branches", branches.Count);
        logger.LogInformation("Testing application for the GitVersion.Core without the command processing");
        return ValueTask.FromResult(0).AsTask();
    }
}
