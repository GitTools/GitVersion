namespace GitVersion;

public static class RepositoryExtensions
{
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
