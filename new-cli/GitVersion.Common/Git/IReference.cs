namespace GitVersion;

public interface IReference : IEquatable<IReference?>, IComparable<IReference>, INamedReference
{
    string TargetIdentifier { get; }
    IObjectId? ReferenceTargetId { get; }
}
