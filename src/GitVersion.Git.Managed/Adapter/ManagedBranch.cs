using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class ManagedBranch : IBranch
{
    private static readonly LambdaEqualityHelper<IBranch> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<IBranch, string> comparerHelper = new(x => x.Name.Canonical);

    private readonly ManagedGitRepository repository;

    internal ManagedBranch(ReferenceName name, ManagedCommit? tip, ManagedGitRepository repository)
    {
        Name = name.NotNull();
        Tip = tip;
        this.repository = repository.NotNull();
        Commits = ManagedCommitCollection.ReachableFrom(repository, tip);
    }

    public ReferenceName Name { get; }
    public ICommit? Tip { get; }
    public ICommitCollection Commits { get; }

    public bool IsDetachedHead => Name.Canonical.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);
    public bool IsRemote => Name.IsRemoteBranch;
    public bool IsTracking => this.repository.Session.IsTracking(this);

    public int CompareTo(IBranch? other) => comparerHelper.Compare(this, other);
    public bool Equals(IBranch? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as IBranch);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
}
