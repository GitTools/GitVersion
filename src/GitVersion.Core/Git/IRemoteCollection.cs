namespace GitVersion.Git;

/// <summary>Represents the set of remotes configured for a Git repository.</summary>
public interface IRemoteCollection : IEnumerable<IRemote>
{
    /// <summary>Returns the remote with the given <paramref name="name"/>, or <see langword="null"/> if it does not exist.</summary>
    IRemote? this[string name] { get; }

    /// <summary>Removes the remote identified by <paramref name="remoteName"/> from the repository configuration.</summary>
    void Remove(string remoteName);

    /// <summary>Adds or updates the fetch refspec for the remote identified by <paramref name="remoteName"/>.</summary>
    void Update(string remoteName, string refSpec);
}
