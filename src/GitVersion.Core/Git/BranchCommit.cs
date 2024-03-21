using GitVersion.Extensions;

namespace GitVersion.Git;

/// <summary>
/// A commit, together with the branch to which the commit belongs.
/// </summary>
[DebuggerDisplay("{Branch} {Commit}")]
public readonly struct BranchCommit(ICommit commit, IBranch branch) : IEquatable<BranchCommit?>
{
    public static readonly BranchCommit Empty = new();

    public IBranch Branch { get; } = branch.NotNull();
    public ICommit Commit { get; } = commit.NotNull();

    public bool Equals(BranchCommit? other)
    {
        if (other is null)
            return false;

        return Equals(Branch, other.Value.Branch) && Equals(Commit, other.Value.Commit);
    }

    public override bool Equals(object? obj) => obj is not null && Equals(obj as BranchCommit?);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Branch?.GetHashCode()) ?? 0) * 397 ^ ((Commit?.GetHashCode()) ?? 0);
        }
    }

    public static bool operator ==(BranchCommit left, BranchCommit right) => left.Equals(right);

    public static bool operator !=(BranchCommit left, BranchCommit right) => !left.Equals(right);
}
