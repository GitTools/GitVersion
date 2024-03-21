namespace GitVersion.Git;

public interface IReference : IEquatable<IReference?>, IComparable<IReference>, INamedReference
{
    string TargetIdentifier { get; }
    IObjectId? ReferenceTargetId { get; }
}
