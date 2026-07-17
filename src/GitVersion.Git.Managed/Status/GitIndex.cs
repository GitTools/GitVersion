using System.Buffers.Binary;

namespace GitVersion.Git;

/// <summary>
/// A single entry of the <c>.git/index</c> file.
/// </summary>
/// <param name="Path">The repository-relative path, using forward slashes.</param>
/// <param name="Mode">The file mode, e.g. <c>0b1000_000_110_100_100</c> (100644 octal) for a regular file.</param>
/// <param name="ObjectId">The object id of the staged blob.</param>
/// <param name="Stage">The merge stage: 0 for a normal entry, 1–3 during conflicts.</param>
/// <param name="Size">The cached on-disk file size (truncated to 32 bits).</param>
/// <param name="ModificationTimeSeconds">The cached modification time, in seconds since the epoch.</param>
/// <param name="ModificationTimeNanoseconds">The nanosecond fraction of the cached modification time.</param>
/// <param name="AssumeValid">Whether the assume-unchanged bit is set.</param>
/// <param name="SkipWorktree">Whether the skip-worktree bit is set (sparse checkout).</param>
/// <param name="IntentToAdd">Whether the intent-to-add bit is set (<c>git add -N</c>).</param>
internal sealed record GitIndexEntry(
    string Path,
    uint Mode,
    GitObjectId ObjectId,
    int Stage,
    uint Size,
    uint ModificationTimeSeconds,
    uint ModificationTimeNanoseconds,
    bool AssumeValid,
    bool SkipWorktree,
    bool IntentToAdd)
{
    /// <summary>
    /// Gets a value indicating whether the entry's file mode has the executable bit set.
    /// </summary>
    public bool IsExecutable => (Mode & 0b001_000_000) != 0;

    /// <summary>
    /// Gets a value indicating whether the entry is a symbolic link (mode <c>120000</c>).
    /// </summary>
    public bool IsSymbolicLink => (Mode & 0b1111_000_000_000_000) == 0b1010_000_000_000_000;

    /// <summary>
    /// Gets a value indicating whether the entry is a gitlink/submodule (mode <c>160000</c>).
    /// </summary>
    public bool IsGitLink => (Mode & 0b1111_000_000_000_000) == 0b1110_000_000_000_000;
}

/// <summary>
/// Reads the <c>.git/index</c> file (the staging area), versions 2 to 4.
/// </summary>
/// <seealso href="https://git-scm.com/docs/gitformat-index"/>
internal sealed class GitIndex
{
    private const int HeaderLength = 12;
    private const int EntryFixedLength = 62;

    private GitIndex(int version, IReadOnlyList<GitIndexEntry> entries)
    {
        Version = version;
        Entries = entries;
    }

    /// <summary>
    /// Gets the format version of the index file: 2, 3 or 4.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the entries of the index, in the order stored (sorted by path and stage).
    /// </summary>
    public IReadOnlyList<GitIndexEntry> Entries { get; }

    /// <summary>
    /// Reads an index file from disk. A missing file yields an empty index.
    /// </summary>
    /// <param name="path">The path to the index file.</param>
    /// <returns>The parsed index.</returns>
    public static GitIndex Read(string path) =>
        File.Exists(path) ? Parse(File.ReadAllBytes(path)) : new(2, []);

    /// <summary>
    /// Parses the binary content of an index file.
    /// </summary>
    /// <param name="data">The raw bytes of the index file.</param>
    /// <returns>The parsed index.</returns>
    public static GitIndex Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderLength || !data[..4].SequenceEqual("DIRC"u8))
        {
            throw new GitObjectStoreException("The index file has an invalid signature.");
        }

        var version = BinaryPrimitives.ReadInt32BigEndian(data[4..8]);

        if (version is < 2 or > 4)
        {
            throw new GitObjectStoreException($"Index version {version} is not supported; only versions 2 to 4 are supported.");
        }

        var entryCount = BinaryPrimitives.ReadInt32BigEndian(data[8..12]);
        var entries = new List<GitIndexEntry>(entryCount);
        var offset = HeaderLength;
        var previousPath = "";

        for (var i = 0; i < entryCount; i++)
        {
            (var entry, offset) = ReadEntry(data, offset, version, previousPath);
            entries.Add(entry);
            previousPath = entry.Path;
        }

        // Extensions and the trailing checksum are not needed and are skipped.
        return new(version, entries);
    }

    private static (GitIndexEntry Entry, int Offset) ReadEntry(ReadOnlySpan<byte> data, int offset, int version, string previousPath)
    {
        var entryStart = offset;
        var fixedPart = data.Slice(offset, EntryFixedLength);

        var modificationSeconds = BinaryPrimitives.ReadUInt32BigEndian(fixedPart[8..12]);
        var modificationNanoseconds = BinaryPrimitives.ReadUInt32BigEndian(fixedPart[12..16]);
        var mode = BinaryPrimitives.ReadUInt32BigEndian(fixedPart[24..28]);
        var size = BinaryPrimitives.ReadUInt32BigEndian(fixedPart[36..40]);
        var objectId = GitObjectId.Parse(fixedPart.Slice(40, GitObjectId.Sha1Size));
        var flags = BinaryPrimitives.ReadUInt16BigEndian(fixedPart[60..62]);

        var assumeValid = (flags & 0x8000) != 0;
        var extended = (flags & 0x4000) != 0;
        var stage = (flags >> 12) & 0x3;
        var nameLength = flags & 0xFFF;

        offset += EntryFixedLength;

        var skipWorktree = false;
        var intentToAdd = false;

        if (extended)
        {
            if (version < 3)
            {
                throw new GitObjectStoreException("The index file is malformed: an extended entry appears in a version 2 index.");
            }

            var extendedFlags = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            skipWorktree = (extendedFlags & 0x4000) != 0;
            intentToAdd = (extendedFlags & 0x2000) != 0;
            offset += 2;
        }

        string path;

        if (version < 4)
        {
            var nameBytes = nameLength < 0xFFF
                ? data.Slice(offset, nameLength)
                : data[offset..(offset + data[offset..].IndexOf((byte)0))];
            path = Encoding.UTF8.GetString(nameBytes);

            // The entry is zero-padded so its total length is a multiple of eight,
            // with at least one trailing NUL.
            var entryLength = ((offset - entryStart) + nameBytes.Length + 8) & ~7;
            offset = entryStart + entryLength;
        }
        else
        {
            // Version 4 prefix-compresses the path against the previous entry's path.
            (var stripCount, offset) = ReadVariableWidthInt(data, offset);
            var suffixEnd = data[offset..].IndexOf((byte)0);
            var suffix = Encoding.UTF8.GetString(data.Slice(offset, suffixEnd));
            path = previousPath[..(previousPath.Length - stripCount)] + suffix;
            offset += suffixEnd + 1;
        }

        var entry = new GitIndexEntry(
            path,
            mode,
            objectId,
            stage,
            size,
            modificationSeconds,
            modificationNanoseconds,
            assumeValid,
            skipWorktree,
            intentToAdd);

        return (entry, offset);
    }

    private static (int Value, int Offset) ReadVariableWidthInt(ReadOnlySpan<byte> data, int offset)
    {
        // Git's offset-encoded variable-width integer: each continuation adds one.
        int current = data[offset++];
        var value = current & 0x7F;

        while ((current & 0x80) != 0)
        {
            value++;
            current = data[offset++];
            value = (value << 7) | (current & 0x7F);
        }

        return (value, offset);
    }
}
