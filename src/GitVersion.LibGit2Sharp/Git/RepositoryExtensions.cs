namespace GitVersion.Git;

internal static class RepositoryExtensions
{
    internal static void RunSafe(Action operation)
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

    internal static T RunSafe<T>(Func<T> operation)
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
