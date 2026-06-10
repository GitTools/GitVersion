namespace GitVersion.Git;

/// <summary>Provides read-only access to a Git repository.</summary>
public interface IGitRepository : IDisposable
{
    /// <summary>Gets the path to the <c>.git</c> directory.</summary>
    string Path { get; }

    /// <summary>Gets the path to the working tree root directory.</summary>
    string WorkingDirectory { get; }

    /// <summary>Gets a value indicating whether HEAD is in a detached state.</summary>
    bool IsHeadDetached { get; }

    /// <summary>Gets a value indicating whether the repository is a shallow clone.</summary>
    bool IsShallow { get; }

    /// <summary>Gets the currently checked-out branch.</summary>
    IBranch Head { get; }

    /// <summary>Gets the collection of all tags in the repository.</summary>
    ITagCollection Tags { get; }

    /// <summary>Gets the collection of all references in the repository.</summary>
    IReferenceCollection References { get; }

    /// <summary>Gets the collection of all branches in the repository.</summary>
    IBranchCollection Branches { get; }

    /// <summary>Gets the collection of all commits reachable from HEAD.</summary>
    ICommitCollection Commits { get; }

    /// <summary>Gets the collection of configured remotes.</summary>
    IRemoteCollection Remotes { get; }

    /// <summary>Finds the best common ancestor between <paramref name="commit"/> and <paramref name="otherCommit"/>.</summary>
    ICommit? FindMergeBase(ICommit commit, ICommit otherCommit);

    /// <summary>Returns the number of files that have been modified but not yet staged or committed.</summary>
    int UncommittedChangesCount();

    /// <summary>Loads the repository located at <paramref name="gitDirectory"/>.</summary>
    void DiscoverRepository(string? gitDirectory);
}
