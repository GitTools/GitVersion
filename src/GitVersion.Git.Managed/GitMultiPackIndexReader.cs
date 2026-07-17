using System.Buffers.Binary;

namespace GitVersion.Git;

/// <summary>
/// Reads a Git <c>multi-pack-index</c> file (format version 1), which indexes the objects
/// of multiple pack files in a single file.
/// </summary>
/// <seealso href="https://git-scm.com/docs/gitformat-pack#_multi_pack_index_midx_files_have_the_following_format"/>
internal sealed class GitMultiPackIndexReader : IDisposable
{
    private const uint PackNamesChunkId = 0x504E414D;     // "PNAM"
    private const uint OidFanoutChunkId = 0x4F494446;     // "OIDF"
    private const uint OidLookupChunkId = 0x4F49444C;     // "OIDL"
    private const uint ObjectOffsetsChunkId = 0x4F4F4646; // "OOFF"

    private const int HeaderLength = 12;
    private const int ChunkTableEntryLength = 12;

    private readonly FileStream stream;
    private readonly int oidLength;
    private readonly int[] fanoutTable = new int[256];
    private readonly Dictionary<uint, (long Offset, long Length)> chunks = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="GitMultiPackIndexReader"/> class.
    /// </summary>
    /// <param name="stream">A <see cref="FileStream"/> which points to the multi-pack-index file. Ownership is transferred to the reader.</param>
    public GitMultiPackIndexReader(FileStream stream)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));

        try
        {
            Span<byte> header = stackalloc byte[HeaderLength];
            stream.SafeFileHandle.ReadExactlyAt(0, header);

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

            ReadChunkTable(chunkCount);
            ReadFanoutTable();
            PackNames = ReadPackNames((int)packCount);
        }
        catch
        {
            this.stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets the names of the pack files indexed by this multi-pack-index, without their
    /// file extension, in the order used by <see cref="GetOffset(GitObjectId)"/>.
    /// </summary>
    public IReadOnlyList<string> PackNames { get; }

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

        var low = objectName[0] == 0 ? 0 : this.fanoutTable[objectName[0] - 1];
        var high = this.fanoutTable[objectName[0]] - 1;

        var oidLookupOffset = GetChunk(OidLookupChunkId).Offset;

        Span<byte> current = stackalloc byte[this.oidLength];
        var order = -1;
        var i = 0;

        while (low <= high)
        {
            i = (low + high) / 2;

            this.stream.SafeFileHandle.ReadExactlyAt(oidLookupOffset + ((long)this.oidLength * i), current);
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

        Span<byte> entry = stackalloc byte[8];
        this.stream.SafeFileHandle.ReadExactlyAt(GetChunk(ObjectOffsetsChunkId).Offset + (8L * i), entry);

        var packIndex = BinaryPrimitives.ReadUInt32BigEndian(entry[..4]);
        var offset = BinaryPrimitives.ReadUInt32BigEndian(entry[4..8]);

        if ((offset & 0x8000_0000) != 0)
        {
            throw new NotSupportedException("Multi-pack-index files with large (>= 2 GiB) object offsets are not supported.");
        }

        return ((int)packIndex, offset);
    }

    /// <inheritdoc/>
    public void Dispose() => this.stream.Dispose();

    private (long Offset, long Length) GetChunk(uint chunkId) =>
        this.chunks.TryGetValue(chunkId, out var chunk)
            ? chunk
            : throw new GitObjectStoreException($"The multi-pack-index file is missing the required 0x{chunkId:X8} chunk.");

    private void ReadChunkTable(int chunkCount)
    {
        // The chunk table has chunkCount + 1 entries; the terminating entry (id 0) records
        // the offset at which the chunks end, which yields each chunk's length.
        var table = new byte[(chunkCount + 1) * ChunkTableEntryLength];
        this.stream.SafeFileHandle.ReadExactlyAt(HeaderLength, table);

        for (var i = 0; i < chunkCount; i++)
        {
            var entry = table.AsSpan(i * ChunkTableEntryLength, ChunkTableEntryLength);
            var chunkId = BinaryPrimitives.ReadUInt32BigEndian(entry[..4]);
            var chunkOffset = BinaryPrimitives.ReadInt64BigEndian(entry[4..12]);
            var nextChunkOffset = BinaryPrimitives.ReadInt64BigEndian(table.AsSpan(((i + 1) * ChunkTableEntryLength) + 4, 8));
            this.chunks[chunkId] = (chunkOffset, nextChunkOffset - chunkOffset);
        }
    }

    private void ReadFanoutTable()
    {
        Span<byte> fanout = stackalloc byte[256 * 4];
        this.stream.SafeFileHandle.ReadExactlyAt(GetChunk(OidFanoutChunkId).Offset, fanout);

        for (var i = 0; i < 256; i++)
        {
            this.fanoutTable[i] = BinaryPrimitives.ReadInt32BigEndian(fanout.Slice(4 * i, 4));
        }
    }

    private string[] ReadPackNames(int packCount)
    {
        var (chunkOffset, chunkLength) = GetChunk(PackNamesChunkId);

        var contents = new byte[chunkLength];
        this.stream.SafeFileHandle.ReadExactlyAt(chunkOffset, contents);

        var names = new string[packCount];
        var span = contents.AsSpan();

        for (var i = 0; i < packCount; i++)
        {
            var nameEnd = span.IndexOf((byte)0);
            if (nameEnd < 0)
            {
                throw new GitObjectStoreException("The multi-pack-index pack name chunk is malformed.");
            }

            names[i] = Path.GetFileNameWithoutExtension(Encoding.UTF8.GetString(span[..nameEnd]));
            span = span[(nameEnd + 1)..];
        }

        return names;
    }
}
