// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// A <see cref="Stream"/> which reads data stored as a loose object in the Git object store.
/// The data is stored as a zlib-compressed stream prefixed with the object type and data length.
/// </summary>
internal sealed class GitObjectStream : GitZLibStream
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitObjectStream"/> class.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> from which to read data.</param>
    public GitObjectStream(Stream stream)
        : base(stream) => ObjectType = ReadObjectTypeAndLength();

    /// <summary>
    /// Gets the object type of this Git object, such as <c>commit</c>, <c>tree</c>, <c>blob</c> or <c>tag</c>.
    /// </summary>
    public string ObjectType { get; }

    private string ReadObjectTypeAndLength()
    {
        Span<byte> buffer = stackalloc byte[16];
        var typeLength = 0;

        while (true)
        {
            var read = ReadByte();
            if (read == -1)
            {
                throw new EndOfStreamException();
            }

            if (read == ' ')
            {
                break;
            }

            if (typeLength >= buffer.Length)
            {
                throw new GitObjectStoreException("Invalid loose object header: the object type is too long.");
            }

            buffer[typeLength++] = (byte)read;
        }

        long length = 0;

        while (true)
        {
            var read = ReadByte();
            if (read == -1)
            {
                throw new EndOfStreamException();
            }

            if (read == 0)
            {
                break;
            }

            if (read is < '0' or > '9')
            {
                throw new GitObjectStoreException("Invalid loose object header: the object length is malformed.");
            }

            length = (10 * length) + (read - '0');
        }

        Initialize(length);
        return GitObjectTypes.Canonicalize(buffer[..typeLength]);
    }
}
