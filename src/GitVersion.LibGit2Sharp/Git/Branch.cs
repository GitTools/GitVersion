using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class Branch : IBranch
{
    private static readonly LambdaEqualityHelper<IBranch> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<IBranch, string> comparerHelper = new(x => x.Name.Canonical);

    private readonly LibGit2Sharp.Branch innerBranch;

    internal Branch(LibGit2Sharp.Branch branch, LibGit2Sharp.Diff diff, GitRepositoryCache repositoryCache)
    {
        diff.NotNull();
        repositoryCache.NotNull();
        this.innerBranch = branch.NotNull();
        Name = new(branch.CanonicalName);

        var commit = this.innerBranch.Tip;
        Tip = commit is null ? null : repositoryCache.GetOrWrap(commit, diff);

        var commits = this.innerBranch.Commits;
        Commits = new CommitCollection(commits, diff, repositoryCache);
    }

    public ReferenceName Name { get; }
    public ICommit? Tip { get; }
    public ICommitCollection Commits { get; }
    public int CompareTo(IBranch? other) => comparerHelper.Compare(this, other);
    public bool Equals(IBranch? other) => equalityHelper.Equals(this, other);
    public bool IsDetachedHead => Name.Canonical.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);
    public bool IsRemote => this.innerBranch.IsRemote;
    public bool IsTracking => this.innerBranch.IsTracking;
    public override bool Equals(object? obj) => Equals(obj as IBranch);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
    public static implicit operator LibGit2Sharp.Branch(Branch d) => d.innerBranch;
}
