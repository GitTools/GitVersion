using System.Collections.Generic;

namespace GitVersion
{
    public interface IBranchCollection : IEnumerable<IBranch>
    {
        IBranch this[string friendlyName] { get; }
        IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude);
        void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName);
    }
}
