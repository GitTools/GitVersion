namespace GitVersion;

public interface ITag : IEquatable<ITag?>, IComparable<ITag>, INamedReference
{
    string TargetSha { get; }
    ICommit? PeeledTargetCommit();
}
