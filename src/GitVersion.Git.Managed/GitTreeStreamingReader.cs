// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers;

namespace GitVersion.Git;

/// <summary>
/// Finds entries in Git tree objects by scanning the raw tree bytes directly,
/// without parsing every entry into a <see cref="GitTree"/> model.
/// </summary>
internal static class GitTreeStreamingReader
{
    /// <summary>
    /// Finds a specific node in a git tree.
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> which represents the git tree.</param>
    /// <param name="name">The name of the node to find, in its UTF-8 representation.</param>
    /// <returns>
    /// The <see cref="GitObjectId"/> of the requested node, or <see cref="GitObjectId.Empty"/> if not found.
    /// </returns>
    public static GitObjectId FindNode(Stream stream, ReadOnlySpan<byte> name)
    {
        ArgumentNullException.ThrowIfNull(stream);

        const int hashLength = GitObjectId.Sha1Size;

        var buffer = ArrayPool<byte>.Shared.Rent(checked((int)stream.Length));

        try
        {
            var contents = buffer.AsSpan(0, (int)stream.Length);
            stream.ReadExactly(contents);

            while (!contents.IsEmpty)
            {
                var modeEnd = contents.IndexOf((byte)' ');
                var nameEnd = contents.IndexOf((byte)0);

                if (modeEnd < 0 || nameEnd < modeEnd || contents.Length < nameEnd + 1 + hashLength)
                {
                    throw new GitObjectStoreException("The tree object is malformed.");
                }

                var currentName = contents[(modeEnd + 1)..nameEnd];

                if (currentName.SequenceEqual(name))
                {
                    return GitObjectId.Parse(contents.Slice(nameEnd + 1, hashLength));
                }

                contents = contents[(nameEnd + 1 + hashLength)..];
            }

            return GitObjectId.Empty;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
