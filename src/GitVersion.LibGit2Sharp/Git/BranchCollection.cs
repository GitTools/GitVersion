using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed class BranchCollection : IBranchCollection
{
    private readonly LibGit2Sharp.BranchCollection innerCollection;
    private readonly Lazy<IReadOnlyCollection<IBranch>> branches;
    private readonly Diff diff;
    private readonly GitRepository repo;

    internal BranchCollection(LibGit2Sharp.BranchCollection collection, Diff diff, GitRepository repo)
    {
        this.innerCollection = collection.NotNull();
        this.branches = new Lazy<IReadOnlyCollection<IBranch>>(() => [.. this.innerCollection.Select(branch => repo.GetOrCreate(branch, diff))]);
        this.diff = diff.NotNull();
        this.repo = repo.NotNull();
    }

    public IEnumerator<IBranch> GetEnumerator()
        => this.branches.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IBranch? this[string name]
    {
        get
        {
            name = name.NotNull();
            var branch = this.innerCollection[name];
            return branch is null ? null : this.repo.GetOrCreate(branch, this.diff);
        }
    }

    public IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude)
    {
        var toExclude = branchesToExclude as IBranch[] ?? [.. branchesToExclude];

        return this.Where(BranchIsNotExcluded);

        bool BranchIsNotExcluded(IBranch branch) => toExclude.All(branchToExclude => !branch.Equals(branchToExclude));
    }

    public void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName)
    {
        var branchToUpdate = (Branch)branch.NotNull();

        this.innerCollection.Update(branchToUpdate, Updater);
        return;

        void Updater(BranchUpdater branchUpdater) =>
            branchUpdater.TrackedBranch = remoteTrackingReferenceName;
    }
}
