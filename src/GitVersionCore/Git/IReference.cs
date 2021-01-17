using System;

namespace GitVersion
{
    public interface IReference : IEquatable<IReference>, IComparable<IReference>, INamedReference
    {
        string TargetIdentifier { get; }
        string DirectReferenceTargetIdentifier { get; }
        IObjectId DirectReferenceTargetId { get; }
    }
}
