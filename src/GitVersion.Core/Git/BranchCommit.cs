using GitVersion.Extensions;

namespace GitVersion.Git;

/// <summary>
/// A commit, together with the branch to which the commit belongs.
/// </summary>
[DebuggerDisplay("{Branch} {Commit}")]
public readonly struct BranchCommit(ICommit commit, IBranch branch) : IEquatable<BranchCommit?>
{
    /// <summary>Represents an absent or unresolved branch/commit pair.</summary>
    public static readonly BranchCommit Empty = new();

    /// <summary>Gets the branch associated with this commit.</summary>
    public IBranch Branch { get; } = branch.NotNull();

    /// <summary>Gets the commit on the branch.</summary>
    public ICommit Commit { get; } = commit.NotNull();

    /// <summary>Returns <see langword="true"/> when <paramref name="other"/> refers to the same branch and commit.</summary>
    public bool Equals(BranchCommit? other)
    {
        if (other is null)
        {
            return false;
        }

        return Equals(Branch, other.Value.Branch) && Equals(Commit, other.Value.Commit);
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="obj"/> is a <see cref="BranchCommit"/> that equals this instance.</summary>
    public override bool Equals(object? obj) => obj is not null && Equals(obj as BranchCommit?);

    /// <summary>Returns a hash code computed from the branch and commit.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return ((Branch?.GetHashCode()) ?? 0) * 397 ^ ((Commit?.GetHashCode()) ?? 0);
        }
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> represent the same branch/commit pair.</summary>
    public static bool operator ==(BranchCommit left, BranchCommit right) => left.Equals(right);

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> represent different branch/commit pairs.</summary>
    public static bool operator !=(BranchCommit left, BranchCommit right) => !left.Equals(right);
}
