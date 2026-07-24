// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Represents a Git commit, as stored in the Git object database. The signatures and the
/// message are kept as raw bytes and decoded lazily: history walks load every traversed
/// commit but only ever need the parents and the committer timestamp.
/// </summary>
internal sealed class GitCommit
{
    private GitSignature? author;
    private GitSignature? committer;
    private string? message;

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
    /// Gets the committer timestamp. Parsed eagerly: the revision walk orders commits by it
    /// and must not pay for decoding the full signatures or the message.
    /// </summary>
    public required DateTimeOffset CommitterWhen { get; init; }

    /// <summary>
    /// Gets the raw bytes of the <c>author</c> header value.
    /// </summary>
    public required byte[] RawAuthor { get; init; }

    /// <summary>
    /// Gets the raw bytes of the <c>committer</c> header value.
    /// </summary>
    public required byte[] RawCommitter { get; init; }

    /// <summary>
    /// Gets the raw bytes of the commit message.
    /// </summary>
    public required byte[] RawMessage { get; init; }

    /// <summary>
    /// Gets the value of the commit's <c>encoding</c> header, if present.
    /// </summary>
    public string? EncodingName { get; init; }

    /// <summary>
    /// Gets the author of this commit, decoded on first access.
    /// </summary>
    public GitSignature Author => this.author ??= GitSignature.Parse(RawAuthor, EncodingName);

    /// <summary>
    /// Gets the committer of this commit, decoded on first access.
    /// </summary>
    public GitSignature Committer => this.committer ??= GitSignature.Parse(RawCommitter, EncodingName);

    /// <summary>
    /// Gets the full commit message, decoded on first access honoring the commit's <c>encoding</c> header.
    /// </summary>
    public string Message => this.message ??= GitTextDecoder.Decode(RawMessage, EncodingName);

    /// <summary>
    /// Creates a copy of this commit with no parents. Used to apply shallow-clone grafts:
    /// commits at the <c>.git/shallow</c> boundary are exposed as parentless the way git and
    /// libgit2 expose them, regardless of the parent headers stored in the object.
    /// </summary>
    /// <returns>The parentless copy, or this instance when it already has no parents.</returns>
    public GitCommit WithoutParents() =>
        Parents.Count == 0
            ? this
            : new()
            {
                Sha = Sha,
                Tree = Tree,
                Parents = [],
                CommitterWhen = CommitterWhen,
                RawAuthor = RawAuthor,
                RawCommitter = RawCommitter,
                RawMessage = RawMessage,
                EncodingName = EncodingName
            };

    /// <inheritdoc/>
    public override string ToString() => $"Git Commit: {Sha}";
}
