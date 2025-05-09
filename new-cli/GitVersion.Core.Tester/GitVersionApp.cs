using GitVersion.Git;
using Microsoft.Extensions.Logging;

namespace GitVersion;

public class GitVersionApp(ILogger logger, IGitRepository repository)
{
#pragma warning disable IDE0060
    public Task<int> RunAsync(string[] args)
#pragma warning restore IDE0060
    {
        repository.DiscoverRepository(Directory.GetCurrentDirectory());
        var branches = repository.Branches.ToList();
        logger.LogInformation("Found {count} branches", branches.Count);
        logger.LogInformation("Testing application for the GitVersion.Core without the command processing");
        return ValueTask.FromResult(0).AsTask();
    }
}
