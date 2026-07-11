// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Represents a Git commit, as stored in the Git object database.
/// </summary>
internal sealed class GitCommit
{
    /// <summary>
    /// Gets the <see cref="GitObjectId"/> which uniquely identifies this commit.
    /// </summary>
    public required GitObjectId Sha { get; init; }

    /// <summary>
    /// Gets the <see cref="GitObjectId"/> of the root tree of this commit.
    /// </summary>
    public required GitObjectId Tree { get; init; }

    /// <summary>
    /// Gets the parents of this commit, in order.
    /// </summary>
    public required IReadOnlyList<GitObjectId> Parents { get; init; }

    /// <summary>
    /// Gets the author of this commit.
    /// </summary>
    public required GitSignature Author { get; init; }

    /// <summary>
    /// Gets the committer of this commit.
    /// </summary>
    public required GitSignature Committer { get; init; }

    /// <summary>
    /// Gets the full commit message, decoded honoring the commit's <c>encoding</c> header.
    /// </summary>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"Git Commit: {Sha}";
}
