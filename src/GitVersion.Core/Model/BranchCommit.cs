namespace GitVersion;

/// <summary>
/// A commit, together with the branch to which the commit belongs.
/// </summary>
public readonly struct BranchCommit : IEquatable<BranchCommit?>
{
    public static readonly BranchCommit Empty = new();

    public BranchCommit(ICommit commit, IBranch branch) : this()
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        Commit = commit ?? throw new ArgumentNullException(nameof(commit));
    }

    public IBranch Branch { get; }
    public ICommit Commit { get; }

    public bool Equals(BranchCommit? other) => Equals(Branch, other?.Branch) && Equals(Commit, other?.Commit);

    public override bool Equals(object obj) => obj is null ? false : Equals(obj as BranchCommit?);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Branch != null ? Branch.GetHashCode() : 0) * 397) ^ (Commit != null ? Commit.GetHashCode() : 0);
        }
    }

    public static bool operator ==(BranchCommit left, BranchCommit right) => left.Equals(right);

    public static bool operator !=(BranchCommit left, BranchCommit right) => !left.Equals(right);
}
