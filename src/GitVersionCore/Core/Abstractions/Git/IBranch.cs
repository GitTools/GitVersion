using System;

namespace GitVersion
{
    public interface IBranch : IEquatable<IBranch>, IComparable<IBranch>
    {
        string CanonicalName { get; }
        string FriendlyName { get; }
        string NameWithoutRemote { get; }
        string NameWithoutOrigin { get; }
        ICommit Tip { get; }
        bool IsRemote { get; }
        bool IsTracking { get; }
        bool IsDetachedHead { get; }
        ICommitCollection Commits { get; }
        bool IsSameBranch(IBranch otherBranch);
    }
}
