using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Git;

/// <summary>
/// Provides read-only access to the objects stored in a Git object database
/// (a <c>.git/objects</c> directory), without relying on native libraries or
/// the <c>git</c> executable.
/// </summary>
/// <remarks>
/// Objects are looked up in the <c>multi-pack-index</c> (when present), in the
/// individual pack files, among the loose objects, and finally in the alternate
/// object databases listed in <c>objects/info/alternates</c> (one level deep).
/// </remarks>
internal sealed class GitObjectStore : IDisposable
{
    private readonly List<GitObjectStore> alternates = [];
    private readonly Dictionary<string, GitPack> packsByName = new(StringComparer.Ordinal);
    private readonly Lazy<string[]> packNames;
    private readonly Lazy<GitMultiPackIndexReader?> multiPackIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitObjectStore"/> class.
    /// </summary>
    /// <param name="objectDirectory">The path to the Git object directory (usually <c>.git/objects</c>).</param>
    public GitObjectStore(string objectDirectory) : this(objectDirectory, followAlternates: true)
    {
    }

    private GitObjectStore(string objectDirectory, bool followAlternates)
    {
        ArgumentNullException.ThrowIfNull(objectDirectory);

        ObjectDirectory = Path.GetFullPath(objectDirectory);
        this.packNames = new(LoadPackNames);
        this.multiPackIndex = new(LoadMultiPackIndex);

        if (followAlternates)
        {
            LoadAlternates();
        }
    }

    /// <summary>
    /// Gets the full path to the Git object directory.
    /// </summary>
    public string ObjectDirectory { get; }

    /// <summary>
    /// Reads the commit with the given object id.
    /// </summary>
    /// <param name="objectId">The object id of the commit.</param>
    /// <returns>The <see cref="GitCommit"/>.</returns>
    public GitCommit GetCommit(GitObjectId objectId)
    {
        using var stream = GetObject(objectId, GitObjectTypes.Commit);
        return GitCommitReader.Read(stream, objectId);
    }

    /// <summary>
    /// Reads the tree with the given object id.
    /// </summary>
    /// <param name="objectId">The object id of the tree.</param>
    /// <returns>The <see cref="GitTree"/>.</returns>
    public GitTree GetTree(GitObjectId objectId)
    {
        using var stream = GetObject(objectId, GitObjectTypes.Tree);
        return GitTreeReader.Read(stream, objectId);
    }

    /// <summary>
    /// Reads the annotated tag with the given object id.
    /// </summary>
    /// <param name="objectId">The object id of the annotated tag.</param>
    /// <returns>The <see cref="GitTag"/>.</returns>
    public GitTag GetTag(GitObjectId objectId)
    {
        using var stream = GetObject(objectId, GitObjectTypes.Tag);
        return GitTagReader.Read(stream, objectId);
    }

    /// <summary>
    /// Opens a stream over the contents of the blob with the given object id.
    /// </summary>
    /// <param name="objectId">The object id of the blob.</param>
    /// <returns>A <see cref="Stream"/> over the blob contents.</returns>
    public Stream GetBlob(GitObjectId objectId) => GetObject(objectId, GitObjectTypes.Blob);

    /// <summary>
    /// Opens a stream over the contents of the object with the given object id.
    /// </summary>
    /// <param name="objectId">The object id of the object to retrieve.</param>
    /// <param name="objectType">The expected object type (<c>commit</c>, <c>tree</c>, <c>blob</c> or <c>tag</c>).</param>
    /// <returns>A <see cref="Stream"/> over the object contents.</returns>
    /// <exception cref="GitObjectStoreException">The object could not be found.</exception>
    public Stream GetObject(GitObjectId objectId, string objectType) =>
        TryGetObjectCore(objectId, objectType)?.Stream
            ?? throw new GitObjectStoreException($"An object of type '{objectType}' with id '{objectId}' could not be found.") { ObjectNotFound = true };

    /// <summary>
    /// Attempts to open a stream over the contents of the object with the given object id.
    /// </summary>
    /// <param name="objectId">The object id of the object to retrieve.</param>
    /// <param name="objectType">The expected object type (<c>commit</c>, <c>tree</c>, <c>blob</c> or <c>tag</c>).</param>
    /// <param name="stream">If found, receives a <see cref="Stream"/> over the object contents.</param>
    /// <returns><see langword="true"/> if the object was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetObject(GitObjectId objectId, string objectType, [NotNullWhen(true)] out Stream? stream)
    {
        stream = TryGetObjectCore(objectId, objectType)?.Stream;
        return stream is not null;
    }

    /// <summary>
    /// Attempts to open a stream over the contents of the object with the given object id,
    /// without knowing its object type up front.
    /// </summary>
    /// <param name="objectId">The object id of the object to retrieve.</param>
    /// <param name="stream">If found, receives a <see cref="Stream"/> over the object contents.</param>
    /// <param name="objectType">If found, receives the object type (<c>commit</c>, <c>tree</c>, <c>blob</c> or <c>tag</c>).</param>
    /// <returns><see langword="true"/> if the object was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetObject(GitObjectId objectId, [NotNullWhen(true)] out Stream? stream, [NotNullWhen(true)] out string? objectType)
    {
        (stream, objectType) = TryGetObjectCore(objectId, objectType: null) is { } result
            ? (result.Stream, result.ObjectType)
            : (null, null);
        return stream is not null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var pack in this.packsByName.Values)
        {
            pack.Dispose();
        }

        this.packsByName.Clear();

        if (this.multiPackIndex.IsValueCreated)
        {
            this.multiPackIndex.Value?.Dispose();
        }

        foreach (var alternate in this.alternates)
        {
            alternate.Dispose();
        }
    }

    private (Stream Stream, string ObjectType)? TryGetObjectCore(GitObjectId objectId, string? objectType)
    {
        // Prefer the multi-pack-index when present.
        if (this.multiPackIndex.Value is { } midx && midx.GetOffset(objectId) is { } location)
        {
            var pack = GetPack(midx.PackNames[location.PackIndex]);

            try
            {
                return pack.GetObject(location.Offset, objectType);
            }
            catch (GitObjectStoreException exception) when (exception.ObjectNotFound)
            {
                // The object exists, but with a different type than requested.
                return null;
            }
        }

        // Fall back to the per-pack .idx files (also covers packs newer than the multi-pack-index).
        foreach (var packName in this.packNames.Value)
        {
            if (GetPack(packName).TryGetObject(objectId, objectType, out var stream, out var actualType))
            {
                return (stream, actualType);
            }
        }

        if (TryGetLooseObject(objectId, objectType) is { } looseObject)
        {
            return looseObject;
        }

        foreach (var alternate in this.alternates)
        {
            if (alternate.TryGetObjectCore(objectId, objectType) is { } fromAlternate)
            {
                return fromAlternate;
            }
        }

        return null;
    }

    private (Stream Stream, string ObjectType)? TryGetLooseObject(GitObjectId objectId, string? objectType)
    {
        var sha = objectId.ToString();
        var path = Path.Combine(ObjectDirectory, sha[..2], sha[2..]);

        if (!FileHelpers.TryOpen(path, out var fileStream))
        {
            return null;
        }

        var objectStream = new GitObjectStream(fileStream);

        if (objectType is not null && objectStream.ObjectType != objectType)
        {
            objectStream.Dispose();
            return null;
        }

        return (objectStream, objectStream.ObjectType);
    }

    private GitPack GetPack(string packName)
    {
        if (!this.packsByName.TryGetValue(packName, out var pack))
        {
            var packDirectory = Path.Combine(ObjectDirectory, "pack");
            pack = new(
                (id, type) => TryGetObjectCore(id, type),
                Path.Combine(packDirectory, packName + ".idx"),
                Path.Combine(packDirectory, packName + ".pack"));
            this.packsByName.Add(packName, pack);
        }

        return pack;
    }

    private string[] LoadPackNames()
    {
        var packDirectory = Path.Combine(ObjectDirectory, "pack");

        if (!Directory.Exists(packDirectory))
        {
            return [];
        }

        return [.. Directory.GetFiles(packDirectory, "*.idx")
            .Where(indexPath => File.Exists(Path.ChangeExtension(indexPath, ".pack")))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];
    }

    private GitMultiPackIndexReader? LoadMultiPackIndex()
    {
        var multiPackIndexPath = Path.Combine(ObjectDirectory, "pack", "multi-pack-index");

        return FileHelpers.TryOpen(multiPackIndexPath, out var stream)
            ? new GitMultiPackIndexReader(stream)
            : null;
    }

    private void LoadAlternates()
    {
        var alternatesPath = Path.Combine(ObjectDirectory, "info", "alternates");

        if (!File.Exists(alternatesPath))
        {
            return;
        }

        foreach (var line in File.ReadLines(alternatesPath))
        {
            var alternatePath = line.Trim();

            if (alternatePath.Length == 0 || alternatePath.StartsWith('#'))
            {
                continue;
            }

            if (!Path.IsPathRooted(alternatePath))
            {
                alternatePath = Path.Combine(ObjectDirectory, alternatePath);
            }

            if (Directory.Exists(alternatePath))
            {
                // Alternates are followed one level deep only.
                this.alternates.Add(new(alternatePath, followAlternates: false));
            }
        }
    }
}
