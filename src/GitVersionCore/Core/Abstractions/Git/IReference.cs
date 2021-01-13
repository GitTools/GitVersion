using System;

namespace GitVersion
{
    public interface IReference : IEquatable<IReference>, IComparable<IReference>
    {
        string CanonicalName { get; }
        string TargetIdentifier { get; }
        string DirectReferenceTargetIdentifier { get; }
        IObjectId DirectReferenceTargetId { get; }
        IReference ResolveToDirectReference();
    }
}
