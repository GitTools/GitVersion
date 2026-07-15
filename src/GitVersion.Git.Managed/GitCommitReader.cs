// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers;

namespace GitVersion.Git;

/// <summary>
/// Reads a <see cref="GitCommit"/> object.
/// </summary>
internal static class GitCommitReader
{
    /// <summary>
    /// Reads a <see cref="GitCommit"/> object from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> which contains the commit in its text representation.</param>
    /// <param name="sha">The <see cref="GitObjectId"/> of the commit.</param>
    /// <returns>The <see cref="GitCommit"/>.</returns>
    public static GitCommit Read(Stream stream, GitObjectId sha)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var buffer = ArrayPool<byte>.Shared.Rent(checked((int)stream.Length));

        try
        {
            var span = buffer.AsSpan(0, (int)stream.Length);
            stream.ReadExactly(span);

            return Read(span, sha);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads a <see cref="GitCommit"/> object from a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="commit">A <see cref="ReadOnlySpan{T}"/> which contains the commit in its text representation.</param>
    /// <param name="sha">The <see cref="GitObjectId"/> of the commit.</param>
    /// <returns>The <see cref="GitCommit"/>.</returns>
    public static GitCommit Read(ReadOnlySpan<byte> commit, GitObjectId sha)
    {
        GitObjectId? tree = null;
        List<GitObjectId> parents = [];
        ReadOnlySpan<byte> authorLine = default;
        ReadOnlySpan<byte> committerLine = default;
        var hasAuthor = false;
        var hasCommitter = false;
        string? encodingName = null;

        var buffer = commit;

        while (!buffer.IsEmpty)
        {
            var lineEnd = buffer.IndexOf((byte)'\n');
            if (lineEnd < 0)
            {
                lineEnd = buffer.Length;
            }

            var line = buffer[..lineEnd];
            buffer = buffer[Math.Min(lineEnd + 1, buffer.Length)..];

            if (line.IsEmpty)
            {
                // An empty line separates the headers from the commit message.
                break;
            }

            if (line[0] == ' ')
            {
                // A continuation of the previous (multi-line) header, such as gpgsig; skip.
                continue;
            }

            if (line.StartsWith("tree "u8))
            {
                tree = GitObjectId.ParseHex(line["tree "u8.Length..]);
            }
            else if (line.StartsWith("parent "u8))
            {
                parents.Add(GitObjectId.ParseHex(line["parent "u8.Length..]));
            }
            else if (line.StartsWith("author "u8))
            {
                authorLine = line["author "u8.Length..];
                hasAuthor = true;
            }
            else if (line.StartsWith("committer "u8))
            {
                committerLine = line["committer "u8.Length..];
                hasCommitter = true;
            }
            else if (line.StartsWith("encoding "u8))
            {
                encodingName = GitTextDecoder.Decode(line["encoding "u8.Length..]);
            }
        }

        if (tree is null || !hasAuthor || !hasCommitter)
        {
            throw new GitObjectStoreException($"The commit {sha} is malformed: a tree, author or committer header is missing.");
        }

        return new()
        {
            Sha = sha,
            Tree = tree.Value,
            Parents = parents,
            CommitterWhen = GitSignature.ParseWhen(committerLine),
            RawAuthor = authorLine.ToArray(),
            RawCommitter = committerLine.ToArray(),
            RawMessage = buffer.ToArray(),
            EncodingName = encodingName
        };
    }
}
