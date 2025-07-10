using GitVersion.Git;
using GitVersion.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GitVersion;

public class GitVersionApp(ILogger<GitVersionApp> logger, IGitRepository repository) : ICliApp
{
    public Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        repository.DiscoverRepository(Directory.GetCurrentDirectory());
        var branches = repository.Branches.ToList();
        logger.LogInformation("Found {count} branches", branches.Count);
        logger.LogInformation("Testing application for the GitVersion.Core without the command processing");
        return ValueTask.FromResult(0).AsTask();
    }
}
