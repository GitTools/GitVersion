namespace GitVersion.Git;

/// <summary>Represents a Git reference (branch tip, tag, or symbolic ref).</summary>
public interface IReference : IEquatable<IReference?>, IComparable<IReference>, INamedReference
{
    /// <summary>Gets the raw string that the reference points to (SHA or another reference name).</summary>
    string TargetIdentifier { get; }

    /// <summary>Gets the resolved object ID that this reference ultimately points to, or <see langword="null"/> for a broken reference.</summary>
    IObjectId? ReferenceTargetId { get; }
}
