namespace GitVersion.Git;

// Keep name-based lookups and strategies that enumerate branches consistent with
// CurrentBranch, including when a real branch with that name points elsewhere.
internal sealed class ContextualBranchCollection(IBranchCollection branches, ContextualBranch context) : IBranchCollection
{
    public IBranch? this[string name] => this.FirstOrDefault(branch => branch.Name.EquivalentTo(name));

    public IEnumerator<IBranch> GetEnumerator() => branches.Where(branch => !branch.Equals(context)).Prepend(context).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude)
        => this.Except(branchesToExclude);

    public void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName)
    {
        if (branch.Equals(context))
        {
            throw new InvalidOperationException("Calculation branch context cannot update repository refs.");
        }

        branches.UpdateTrackedBranch(branch, remoteTrackingReferenceName);
    }
}
