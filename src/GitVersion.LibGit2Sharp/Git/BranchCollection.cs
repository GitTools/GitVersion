using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed class BranchCollection : IBranchCollection
{
    private readonly LibGit2Sharp.BranchCollection innerCollection;

    internal BranchCollection(LibGit2Sharp.BranchCollection collection)
        => this.innerCollection = collection.NotNull();

    public IEnumerator<IBranch> GetEnumerator()
        => this.innerCollection.Select(branch => new Branch(branch)).GetEnumerator();

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

        return this.Where(BranchIsNotExcluded);

        bool BranchIsNotExcluded(IBranch branch)
            => branchesToExclude.All(branchToExclude => !branch.Equals(branchToExclude));
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
