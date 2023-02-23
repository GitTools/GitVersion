using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion;

internal sealed class BranchCollection : IBranchCollection
{
    private readonly LibGit2Sharp.BranchCollection innerCollection;
    internal BranchCollection(LibGit2Sharp.BranchCollection collection) => this.innerCollection = collection.NotNull();

    public IEnumerator<IBranch> GetEnumerator() => this.innerCollection.Select(branch => new Branch(branch)).GetEnumerator();
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
        branchesToExclude = branchesToExclude.NotNull();

        bool BranchIsNotExcluded(IBranch branch) => branchesToExclude.All(branchToExclude => !branch.Equals(branchToExclude));

        return this.Where(BranchIsNotExcluded);
    }
    public void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName)
    {
        var branchToUpdate = (Branch)branch.NotNull();

        void Updater(BranchUpdater branchUpdater) => branchUpdater.TrackedBranch = remoteTrackingReferenceName;

        this.innerCollection.Update(branchToUpdate, Updater);
    }
}
