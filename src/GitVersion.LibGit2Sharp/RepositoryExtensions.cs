using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion;

public static class RepositoryExtensions
{
    public static IGitRepository ToGitRepository(this IRepository repository) => new GitRepository(repository);
    public static IGitRepositoryInfo ToGitRepositoryInfo(IOptions<GitVersionOptions> options) => new GitRepositoryInfo(options);

    public static void RunSafe(Action operation)
    {
        try
        {
            operation();
        }
        catch (LibGit2Sharp.LockedFileException ex)
        {
            // Wrap this exception so that callers that want to catch it don't need to take a dependency on LibGit2Sharp.
            throw new LockedFileException(ex);
        }
    }

    public static T RunSafe<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (LibGit2Sharp.LockedFileException ex)
        {
            // Wrap this exception so that callers that want to catch it don't need to take a dependency on LibGit2Sharp.
            throw new LockedFileException(ex);
        }
    }
}
