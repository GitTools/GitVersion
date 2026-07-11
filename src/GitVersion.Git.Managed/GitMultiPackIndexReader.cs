using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;

namespace GitVersion.Git;

/// <summary>
/// Reads a Git <c>multi-pack-index</c> file (format version 1), which indexes the objects
/// of multiple pack files in a single file.
/// </summary>
/// <seealso href="https://git-scm.com/docs/gitformat-pack#_multi_pack_index_midx_files_have_the_following_format"/>
internal sealed unsafe class GitMultiPackIndexReader : IDisposable
{
    private const uint PackNamesChunkId = 0x504E414D;     // "PNAM"
    private const uint OidFanoutChunkId = 0x4F494446;     // "OIDF"
    private const uint OidLookupChunkId = 0x4F49444C;     // "OIDL"
    private const uint ObjectOffsetsChunkId = 0x4F4F4646; // "OOFF"
    private const uint LargeOffsetsChunkId = 0x4C4F4646;  // "LOFF"

    private const int HeaderLength = 12;
    private const int ChunkTableEntryLength = 12;

    private readonly MemoryMappedFile file;
    private readonly MemoryMappedViewAccessor accessor;
    private readonly int oidLength;
    private readonly int[] fanoutTable = new int[256];
    private readonly Dictionary<uint, long> chunkOffsets = [];
    private byte* ptr;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitMultiPackIndexReader"/> class.
    /// </summary>
    /// <param name="stream">A <see cref="FileStream"/> which points to the multi-pack-index file.</param>
    public GitMultiPackIndexReader(FileStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        this.file = MemoryMappedFile.CreateFromFile(stream, mapName: null, capacity: 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);
        this.accessor = this.file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        this.accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref this.ptr);

        try
        {
            var header = GetSpan(0, HeaderLength);

            if (!header[..4].SequenceEqual("MIDX"u8))
            {
                throw new GitObjectStoreException("The multi-pack-index file has an invalid signature.");
            }

            var version = header[4];
            if (version != 1)
            {
                throw new GitObjectStoreException($"Multi-pack-index version {version} is not supported; only version 1 is supported.");
            }

            this.oidLength = header[5] switch
            {
                1 => GitObjectId.Sha1Size,
                2 => GitObjectId.Sha256Size,
                _ => throw new GitObjectStoreException($"The multi-pack-index object id version {header[5]} is not supported.")
            };

            var chunkCount = header[6];
            var baseFileCount = header[7];
            var packCount = BinaryPrimitives.ReadUInt32BigEndian(header[8..12]);

            if (baseFileCount != 0)
            {
                throw new NotSupportedException("Incremental multi-pack-index chains are not supported.");
            }

            var chunkTable = GetSpan(HeaderLength, (chunkCount + 1) * ChunkTableEntryLength);

            for (var i = 0; i < chunkCount; i++)
            {
                var entry = chunkTable.Slice(i * ChunkTableEntryLength, ChunkTableEntryLength);
                var chunkId = BinaryPrimitives.ReadUInt32BigEndian(entry[..4]);
                var chunkOffset = BinaryPrimitives.ReadInt64BigEndian(entry[4..12]);
                this.chunkOffsets[chunkId] = chunkOffset;
            }

            var fanout = GetSpan(GetChunkOffset(OidFanoutChunkId), 256 * 4);

            for (var i = 0; i < 256; i++)
            {
                this.fanoutTable[i] = BinaryPrimitives.ReadInt32BigEndian(fanout.Slice(4 * i, 4));
            }

            PackNames = ReadPackNames((int)packCount);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets the names of the pack files indexed by this multi-pack-index, without their
    /// file extension, in the order used by <see cref="GetOffset(GitObjectId)"/>.
    /// </summary>
    public IReadOnlyList<string> PackNames { get; } = [];

    /// <summary>
    /// Looks up an object in the multi-pack-index.
    /// </summary>
    /// <param name="objectId">The Git object id of the object to look up.</param>
    /// <returns>
    /// If found, the index (into <see cref="PackNames"/>) of the pack which contains the object,
    /// and the offset of the object within that pack; otherwise, <see langword="null"/>.
    /// </returns>
    public (int PackIndex, long Offset)? GetOffset(GitObjectId objectId)
    {
        if (objectId.HashLength != this.oidLength)
        {
            return null;
        }

        Span<byte> objectName = stackalloc byte[this.oidLength];
        objectId.CopyTo(objectName);

        var start = objectName[0] == 0 ? 0 : this.fanoutTable[objectName[0] - 1];
        var end = this.fanoutTable[objectName[0]] - 1;

        if (end < start)
        {
            return null;
        }

        var oidLookupOffset = GetChunkOffset(OidLookupChunkId);
        var table = GetSpan(oidLookupOffset + ((long)this.oidLength * start), this.oidLength * (end - start + 1));

        var order = -1;
        var i = 0;

        var low = 0;
        var high = end - start;

        while (low <= high)
        {
            i = (low + high) / 2;

            order = table.Slice(this.oidLength * i, this.oidLength).SequenceCompareTo(objectName);

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

        var objectIndex = start + i;
        var entry = GetSpan(GetChunkOffset(ObjectOffsetsChunkId) + (8L * objectIndex), 8);
        var packIndex = BinaryPrimitives.ReadUInt32BigEndian(entry[..4]);
        var offset = BinaryPrimitives.ReadUInt32BigEndian(entry[4..8]);

        if ((offset & 0x8000_0000) != 0)
        {
            throw new NotSupportedException("Multi-pack-index files with large (>= 2 GiB) object offsets are not supported.");
        }

        return ((int)packIndex, offset);
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

    private long GetChunkOffset(uint chunkId) =>
        this.chunkOffsets.TryGetValue(chunkId, out var offset)
            ? offset
            : throw new GitObjectStoreException($"The multi-pack-index file is missing the required 0x{chunkId:X8} chunk.");

    private string[] ReadPackNames(int packCount)
    {
        var names = new string[packCount];
        var offset = GetChunkOffset(PackNamesChunkId);

        for (var i = 0; i < packCount; i++)
        {
            var length = 0;

            while (GetSpan(offset + length, 1)[0] != 0)
            {
                length++;
            }

            var name = Encoding.UTF8.GetString(GetSpan(offset, length));
            names[i] = Path.GetFileNameWithoutExtension(name);
            offset += length + 1;
        }

        return names;
    }

    private ReadOnlySpan<byte> GetSpan(long offset, int length) => new(this.ptr + offset, length);
}
