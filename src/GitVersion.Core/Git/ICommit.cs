namespace GitVersion.Git;

/// <summary>Represents a single Git commit.</summary>
public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>, ICommitish
{
    /// <summary>Gets the direct parent commits of this commit.</summary>
    IReadOnlyList<ICommit> Parents { get; }

    /// <summary>Gets the object identifier (SHA) of this commit.</summary>
    IObjectId Id { get; }

    /// <summary>Gets the full SHA-1 hash string of this commit.</summary>
    string Sha { get; }

    /// <summary>Gets the author date of this commit.</summary>
    DateTimeOffset When { get; }

    /// <summary>Gets the full commit message.</summary>
    string Message { get; }

    /// <summary>Gets a value indicating whether this is a merge commit (has more than one parent).</summary>
    bool IsMergeCommit { get; }

    /// <summary>Gets the paths of files changed in this commit relative to its first parent.</summary>
    IReadOnlyList<string> DiffPaths { get; }
}
