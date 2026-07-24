namespace GitVersion.Git;

/// <summary>
/// Represents a Git reference: either a direct reference pointing at an object,
/// or a symbolic reference pointing at another reference.
/// </summary>
internal sealed class GitReference
{
    /// <summary>
    /// Gets the canonical name of the reference, e.g. <c>refs/heads/main</c> or <c>HEAD</c>.
    /// </summary>
    public required string CanonicalName { get; init; }

    /// <summary>
    /// Gets the object id the reference points at, for direct references.
    /// </summary>
    public GitObjectId? ObjectId { get; init; }

    /// <summary>
    /// Gets the canonical name of the reference this reference points at, for symbolic references.
    /// </summary>
    public string? SymbolicTargetName { get; init; }

    /// <summary>
    /// Gets the fully peeled target of the reference (the commit an annotated tag ultimately
    /// points at), when known from the <c>packed-refs</c> file; otherwise <see langword="null"/>.
    /// </summary>
    public GitObjectId? PeeledObjectId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this reference was read from the <c>packed-refs</c>
    /// file (as opposed to a loose reference file).
    /// </summary>
    public bool IsPacked { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a symbolic reference.
    /// </summary>
    public bool IsSymbolic => SymbolicTargetName is not null;

    /// <inheritdoc/>
    public override string ToString() =>
        IsSymbolic
            ? $"{CanonicalName} -> {SymbolicTargetName}"
            : $"{CanonicalName} -> {ObjectId}";
}
