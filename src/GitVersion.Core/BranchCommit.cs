using GitVersion.Extensions;

namespace GitVersion;

/// <summary>
/// A commit, together with the branch to which the commit belongs.
/// </summary>
public readonly struct BranchCommit : IEquatable<BranchCommit?>
{
    public static readonly BranchCommit Empty = new();

    public BranchCommit(ICommit commit, IBranch branch) : this()
    {
        Branch = branch.NotNull();
        Commit = commit.NotNull();
    }

    public IBranch Branch { get; }
    public ICommit Commit { get; }

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
            return (Branch != null ? Branch.GetHashCode() : 0) * 397 ^ (Commit != null ? Commit.GetHashCode() : 0);
        }
    }

    public static bool operator ==(BranchCommit left, BranchCommit right) => left.Equals(right);

    public static bool operator !=(BranchCommit left, BranchCommit right) => !left.Equals(right);
}
