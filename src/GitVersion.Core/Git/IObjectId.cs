namespace GitVersion.Git;

public interface IObjectId : IEquatable<IObjectId?>, IComparable<IObjectId>
{
    string Sha { get; }
    string ToString(int prefixLength);
}
