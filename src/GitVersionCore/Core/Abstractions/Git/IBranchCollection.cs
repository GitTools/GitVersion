using System.Collections.Generic;

namespace GitVersion
{
    public interface IBranchCollection : IEnumerable<IBranch>
    {
        IBranch this[string friendlyName] { get; }
        void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName);
    }
}
