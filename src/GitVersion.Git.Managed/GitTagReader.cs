// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers;

namespace GitVersion.Git;

/// <summary>
/// Reads a <see cref="GitTag"/> (annotated tag) object.
/// </summary>
internal static class GitTagReader
{
    /// <summary>
    /// Reads a <see cref="GitTag"/> object from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> which contains the tag in its text representation.</param>
    /// <param name="sha">The <see cref="GitObjectId"/> of the tag.</param>
    /// <returns>The <see cref="GitTag"/>.</returns>
    public static GitTag Read(Stream stream, GitObjectId sha)
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
    /// Reads a <see cref="GitTag"/> object from a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="tag">A <see cref="ReadOnlySpan{T}"/> which contains the tag in its text representation.</param>
    /// <param name="sha">The <see cref="GitObjectId"/> of the tag.</param>
    /// <returns>The <see cref="GitTag"/>.</returns>
    public static GitTag Read(ReadOnlySpan<byte> tag, GitObjectId sha)
    {
        GitObjectId? target = null;
        string? targetType = null;
        string? name = null;
        GitSignature? tagger = null;
        ReadOnlySpan<byte> taggerLine = default;
        var hasTagger = false;

        var buffer = tag;

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
                // An empty line separates the headers from the tag message.
                break;
            }

            if (line[0] == ' ')
            {
                // A continuation of the previous (multi-line) header; skip.
                continue;
            }

            if (line.StartsWith("object "u8))
            {
                target = GitObjectId.ParseHex(line["object "u8.Length..]);
            }
            else if (line.StartsWith("type "u8))
            {
                targetType = GitObjectTypes.Canonicalize(line["type "u8.Length..]);
            }
            else if (line.StartsWith("tag "u8))
            {
                name = GitTextDecoder.Decode(line["tag "u8.Length..]);
            }
            else if (line.StartsWith("tagger "u8))
            {
                taggerLine = line["tagger "u8.Length..];
                hasTagger = true;
            }
        }

        if (target is null || targetType is null || name is null)
        {
            throw new GitObjectStoreException($"The tag {sha} is malformed: an object, type or tag header is missing.");
        }

        if (hasTagger)
        {
            tagger = GitSignature.Parse(taggerLine);
        }

        return new()
        {
            Sha = sha,
            Target = target.Value,
            TargetType = targetType,
            Name = name,
            Tagger = tagger,
            Message = GitTextDecoder.Decode(buffer)
        };
    }
}
