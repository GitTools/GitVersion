namespace GitVersion;

public interface IBranchCollection : IEnumerable<IBranch>
{
    IBranch? this[string name] { get; }
    IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude);
    void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName);
}
