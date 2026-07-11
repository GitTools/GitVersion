// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;

namespace GitVersion.Git;

/// <summary>
/// Reads a Git pack index (<c>.idx</c>) file, version 2, using a memory-mapped file.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format"/>
internal sealed unsafe class GitPackIndexReader : IDisposable
{
    private static readonly byte[] Header = [0xff, 0x74, 0x4f, 0x63];

    private readonly MemoryMappedFile file;
    private readonly MemoryMappedViewAccessor accessor;

    // The fanout table consists of 256 4-byte network byte order integers.
    // The N-th entry of this table records the number of objects in the corresponding pack,
    // the first byte of whose object name is less than or equal to N.
    private readonly int[] fanoutTable = new int[257];
    private byte* ptr;

    private bool initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitPackIndexReader"/> class.
    /// </summary>
    /// <param name="stream">A <see cref="FileStream"/> which points to the index file.</param>
    public GitPackIndexReader(FileStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        this.file = MemoryMappedFile.CreateFromFile(stream, mapName: null, capacity: 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);
        this.accessor = this.file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        this.accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref this.ptr);
    }

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

        var packStart = this.fanoutTable[objectName[0]];
        var packEnd = this.fanoutTable[objectName[0] + 1] - 1;
        var objectCount = this.fanoutTable[256];

        // The fanout table is followed by a table of sorted 20-byte SHA-1 object names.
        // These are packed together without offset values to reduce the cache footprint
        // of the binary search for a specific object name.
        // The object names start at: 4 (header) + 4 (version) + 256 * 4 (fanout table).
        var tableSize = hashLength * (packEnd - packStart + 1);
        if (tableSize <= 0)
        {
            return null;
        }

        var table = GetSpan(4 + 4 + (256 * 4) + (hashLength * packStart), tableSize);

        var originalPackStart = packStart;
        packEnd -= originalPackStart;
        packStart = 0;

        var i = 0;
        var order = -1;

        while (packStart <= packEnd)
        {
            i = (packStart + packEnd) / 2;

            order = table.Slice(hashLength * i, hashLength).SequenceCompareTo(objectName);

            if (order < 0)
            {
                packStart = i + 1;
            }
            else if (order > 0)
            {
                packEnd = i - 1;
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
        var offsetTableStart = 4 + 4 + (256 * 4) + (hashLength * objectCount) + (4 * objectCount);
        var offsetBuffer = GetSpan(offsetTableStart + (4 * (i + originalPackStart)), 4);
        var offset = BinaryPrimitives.ReadUInt32BigEndian(offsetBuffer);

        if (offsetBuffer[0] < 128)
        {
            return offset;
        }

        // If the first bit of the offset address is set, the offset is stored as a 64-bit value in the table
        // of 8-byte offset entries, which follows the table of 4-byte offset entries:
        // "large offsets are encoded as an index into the next table with the msbit set."
        offset &= 0x7FFFFFFF;

        offsetBuffer = GetSpan(offsetTableStart + (4 * objectCount) + (8 * (int)offset), 8);
        return BinaryPrimitives.ReadInt64BigEndian(offsetBuffer);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.ptr is not null)
        {
            this.accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            this.ptr = null;
        }

        this.accessor.Dispose();
        this.file.Dispose();
    }

    private ReadOnlySpan<byte> GetSpan(long offset, int length) => new(this.ptr + offset, length);

    private void Initialize()
    {
        if (this.initialized)
        {
            return;
        }

        const int fanoutTableLength = 256;
        var value = GetSpan(0, 4 + (4 * fanoutTableLength) + 4);

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
