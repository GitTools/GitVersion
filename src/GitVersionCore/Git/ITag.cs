using System;

namespace GitVersion
{
    public interface ITag : IEquatable<ITag>, IComparable<ITag>
    {
        string TargetSha { get; }
        string FriendlyName { get; }
        string CanonicalName { get; }
        ICommit PeeledTargetCommit();
    }
}
