using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed class BranchCollection : IBranchCollection
{
    private readonly LibGit2Sharp.BranchCollection innerCollection;
    private readonly Lazy<IReadOnlyCollection<IBranch>> branches;

    internal BranchCollection(LibGit2Sharp.BranchCollection collection)
    {
        this.innerCollection = collection.NotNull();
        this.branches = new Lazy<IReadOnlyCollection<IBranch>>(() => this.innerCollection.Select(branch => new Branch(branch)).ToArray());
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
            return branch is null ? null : new Branch(branch);
        }
    }

    public IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude)
    {
        var toExclude = branchesToExclude as IBranch[] ?? branchesToExclude.ToArray();
        bool BranchIsNotExcluded(IBranch branch) => toExclude.All(branchToExclude => !branch.Equals(branchToExclude));

        return this.Where(BranchIsNotExcluded);
    }

    public void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName)
    {
        var branchToUpdate = (Branch)branch.NotNull();

        void Updater(BranchUpdater branchUpdater) =>
            branchUpdater.TrackedBranch = remoteTrackingReferenceName;

        this.innerCollection.Update(branchToUpdate, Updater);
    }
}
