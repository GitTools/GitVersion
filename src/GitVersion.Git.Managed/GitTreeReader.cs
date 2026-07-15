// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers;

namespace GitVersion.Git;

/// <summary>
/// Reads a <see cref="GitTree"/> object.
/// </summary>
internal static class GitTreeReader
{
    /// <summary>
    /// Reads a <see cref="GitTree"/> object from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> which contains the tree in its binary representation.</param>
    /// <param name="objectId">The <see cref="GitObjectId"/> of the tree.</param>
    /// <returns>The <see cref="GitTree"/>.</returns>
    public static GitTree Read(Stream stream, GitObjectId objectId)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var buffer = ArrayPool<byte>.Shared.Rent(checked((int)stream.Length));

        try
        {
            var contents = buffer.AsSpan(0, (int)stream.Length);
            stream.ReadExactly(contents);

            return Read(contents, objectId);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads a <see cref="GitTree"/> object from a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="tree">A <see cref="ReadOnlySpan{T}"/> which contains the tree in its binary representation.</param>
    /// <param name="objectId">The <see cref="GitObjectId"/> of the tree.</param>
    /// <returns>The <see cref="GitTree"/>.</returns>
    public static GitTree Read(ReadOnlySpan<byte> tree, GitObjectId objectId)
    {
        List<GitTreeEntry> entries = [];

        var contents = tree;

        while (!contents.IsEmpty)
        {
            // Format: [mode] [file/folder name]\0[object id of the referenced blob or tree]
            var (name, nameBytes, mode, sha, entryLength) = ReadEntry(contents);
            entries.Add(new(name, nameBytes, mode, sha));
            contents = contents[entryLength..];
        }

        return new()
        {
            Sha = objectId,
            Entries = entries
        };
    }

    internal static (string Name, byte[] NameBytes, string Mode, GitObjectId Sha, int EntryLength) ReadEntry(ReadOnlySpan<byte> contents)
    {
        const int hashLength = GitObjectId.Sha1Size;

        var modeEnd = contents.IndexOf((byte)' ');
        var nameEnd = contents.IndexOf((byte)0);

        if (modeEnd < 0 || nameEnd < modeEnd || contents.Length < nameEnd + 1 + hashLength)
        {
            throw new GitObjectStoreException("The tree object is malformed.");
        }

        var mode = Encoding.UTF8.GetString(contents[..modeEnd]);
        // Keep a stable copy of the raw name bytes: the source span may be backed by a
        // pooled buffer, and the decoded name (UTF-8 with a Latin-1 fallback) does not
        // always round-trip back to the on-disk bytes git sorts entries by.
        var nameBytes = contents[(modeEnd + 1)..nameEnd].ToArray();
        var name = GitTextDecoder.Decode(nameBytes);
        var sha = GitObjectId.Parse(contents.Slice(nameEnd + 1, hashLength));

        return (name, nameBytes, mode, sha, nameEnd + 1 + hashLength);
    }
}
