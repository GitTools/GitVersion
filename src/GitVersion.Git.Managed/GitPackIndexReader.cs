// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers.Binary;

namespace GitVersion.Git;

/// <summary>
/// Reads a Git pack index (<c>.idx</c>) file, version 2.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format"/>
internal sealed class GitPackIndexReader : IDisposable
{
    private static readonly byte[] Header = [0xff, 0x74, 0x4f, 0x63];

    // The object name table starts at: 4 (header) + 4 (version) + 256 * 4 (fanout table).
    private const int NameTableStart = 4 + 4 + (256 * 4);

    private readonly FileStream stream;

    // The fanout table consists of 256 4-byte network byte order integers.
    // The N-th entry of this table records the number of objects in the corresponding pack,
    // the first byte of whose object name is less than or equal to N.
    private readonly int[] fanoutTable = new int[257];

    private bool initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitPackIndexReader"/> class.
    /// </summary>
    /// <param name="stream">A <see cref="FileStream"/> which points to the index file. Ownership is transferred to the reader.</param>
    public GitPackIndexReader(FileStream stream) => this.stream = stream ?? throw new ArgumentNullException(nameof(stream));

    /// <summary>
    /// Gets the offset of a Git object in the pack file.
    /// </summary>
    /// <param name="objectId">The Git object id of the Git object for which to get the offset.</param>
    /// <returns>If found, the offset of the Git object in the pack file; otherwise, <see langword="null"/>.</returns>
    public long? GetOffset(GitObjectId objectId)
    {
        const int hashLength = GitObjectId.Sha1Size;

        Initialize();

        Span<byte> objectName = stackalloc byte[hashLength];
        objectId.CopyTo(objectName);

        var objectCount = this.fanoutTable[256];

        // The fanout table is followed by a table of sorted 20-byte SHA-1 object names.
        var low = this.fanoutTable[objectName[0]];
        var high = this.fanoutTable[objectName[0] + 1] - 1;

        Span<byte> current = stackalloc byte[hashLength];
        var order = -1;
        var i = 0;

        while (low <= high)
        {
            i = (low + high) / 2;

            this.stream.SafeFileHandle.ReadExactlyAt(NameTableStart + ((long)hashLength * i), current);
            order = current.SequenceCompareTo(objectName);

            if (order < 0)
            {
                low = i + 1;
            }
            else if (order > 0)
            {
                high = i - 1;
            }
            else
            {
                break;
            }
        }

        if (order != 0)
        {
            return null;
        }

        // Get the offset value. It's located at:
        // 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * objectCount (name table) + 4 * objectCount (CRC32) + 4 * i (offset values)
        var offsetTableStart = NameTableStart + (hashLength * objectCount) + (4 * objectCount);
        Span<byte> offsetBuffer = stackalloc byte[8];

        this.stream.SafeFileHandle.ReadExactlyAt(offsetTableStart + (4L * i), offsetBuffer[..4]);
        var offset = BinaryPrimitives.ReadUInt32BigEndian(offsetBuffer[..4]);

        if ((offset & 0x8000_0000) == 0)
        {
            return offset;
        }

        // If the first bit of the offset address is set, the offset is stored as a 64-bit value in the table
        // of 8-byte offset entries, which follows the table of 4-byte offset entries:
        // "large offsets are encoded as an index into the next table with the msbit set."
        offset &= 0x7FFFFFFF;

        this.stream.SafeFileHandle.ReadExactlyAt(offsetTableStart + (4L * objectCount) + (8L * offset), offsetBuffer);
        return BinaryPrimitives.ReadInt64BigEndian(offsetBuffer);
    }

    /// <inheritdoc/>
    public void Dispose() => this.stream.Dispose();

    private void Initialize()
    {
        if (this.initialized)
        {
            return;
        }

        const int fanoutTableLength = 256;
        Span<byte> value = stackalloc byte[4 + 4 + (4 * fanoutTableLength)];
        this.stream.SafeFileHandle.ReadExactlyAt(0, value);

        var header = value[..4];
        var version = BinaryPrimitives.ReadInt32BigEndian(value.Slice(4, 4));

        if (!header.SequenceEqual(Header))
        {
            throw new GitObjectStoreException("The pack index file has an invalid header.");
        }

        if (version != 2)
        {
            throw new GitObjectStoreException($"Pack index version {version} is not supported; only version 2 is supported.");
        }

        for (var i = 1; i <= fanoutTableLength; i++)
        {
            this.fanoutTable[i] = BinaryPrimitives.ReadInt32BigEndian(value.Slice(4 + (4 * i), 4));
        }

        this.initialized = true;
    }
}
