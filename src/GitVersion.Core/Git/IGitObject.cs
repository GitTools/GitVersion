namespace GitVersion.Git;

public interface IGitObject : IEquatable<IGitObject?>, IComparable<IGitObject>
{
    IObjectId Id { get; }
    string Sha { get; }
}
