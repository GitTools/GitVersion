using GitVersion.Helpers;

namespace GitVersion.Git;

// A calculation-only branch. The existing ref (if any) remains untouched.
internal sealed class ContextualBranch(ReferenceName name, IBranch head) : IBranch
{
    private static readonly LambdaEqualityHelper<IBranch> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<IBranch, string> comparerHelper = new(x => x.Name.Canonical);

    public ReferenceName Name { get; } = name;
    public ICommit? Tip { get; } = head.Tip;
    public ICommitCollection Commits { get; } = head.Commits;
    public bool IsRemote => false;
    public bool IsTracking => false;
    public bool IsDetachedHead => false;
    public int CompareTo(IBranch? other) => comparerHelper.Compare(this, other);
    public bool Equals(IBranch? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as IBranch);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
}
