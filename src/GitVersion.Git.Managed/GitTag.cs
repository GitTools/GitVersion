// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Represents a Git annotated tag, as stored in the Git object database.
/// </summary>
internal sealed class GitTag
{
    /// <summary>
    /// Gets the <see cref="GitObjectId"/> which uniquely identifies this annotated tag.
    /// </summary>
    public required GitObjectId Sha { get; init; }

    /// <summary>
    /// Gets the <see cref="GitObjectId"/> of the object this tag points at.
    /// </summary>
    public required GitObjectId Target { get; init; }

    /// <summary>
    /// Gets the type of the object this tag points at, e.g. <c>commit</c> or, for nested tags, <c>tag</c>.
    /// </summary>
    public required string TargetType { get; init; }

    /// <summary>
    /// Gets the name of this tag.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the tagger of this tag, or <see langword="null"/> when the tag has no <c>tagger</c> header.
    /// </summary>
    public GitSignature? Tagger { get; init; }

    /// <summary>
    /// Gets the tag message.
    /// </summary>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"Git Tag: {Name} with id {Sha}";
}
