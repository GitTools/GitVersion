// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;

namespace GitVersion.Git;

/// <summary>
/// A delegate which resolves a Git object across the whole object store (used to resolve
/// the base object of <c>ref-delta</c> deltified objects, which may live outside the pack).
/// </summary>
/// <param name="objectId">The Git object id of the object to fetch.</param>
/// <param name="objectType">The expected object type, or <see langword="null"/> to accept any type.</param>
/// <returns>A stream over the object data and its actual object type, or <see langword="null"/> if not found.</returns>
internal delegate (Stream Stream, string ObjectType)? ResolveGitObject(GitObjectId objectId, string? objectType);

/// <summary>
/// Supports retrieving objects from a Git pack file.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format"/>
internal sealed class GitPack : IDisposable
{
    private readonly ResolveGitObject resolveGitObject;
    private readonly Func<FileStream> packStream;
    private readonly Lazy<FileStream> indexStream;
    private readonly GitPackCache cache;
    private readonly Lazy<(MemoryMappedFile File, MemoryMappedViewAccessor Accessor)?> packFile;

    // Maps GitObjectIds to offsets in the git pack.
    private readonly Dictionary<GitObjectId, long> offsets = [];

    private readonly Lazy<GitPackIndexReader> indexReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitPack"/> class.
    /// </summary>
    /// <param name="resolveGitObject">A delegate which fetches objects from the Git object store.</param>
    /// <param name="indexPath">The full path to the index (<c>.idx</c>) file.</param>
    /// <param name="packPath">The full path to the pack file.</param>
    /// <param name="cache">A <see cref="GitPackCache"/> which is used to cache <see cref="Stream"/> objects which operate on the pack file.</param>
    public GitPack(ResolveGitObject resolveGitObject, string indexPath, string packPath, GitPackCache? cache = null)
    {
        this.resolveGitObject = resolveGitObject ?? throw new ArgumentNullException(nameof(resolveGitObject));
        this.indexStream = new(() => File.OpenRead(indexPath));
        this.packStream = () => File.OpenRead(packPath);
        this.indexReader = new(() => new GitPackIndexReader(this.indexStream.Value));
        this.cache = cache ?? new GitPackMemoryCache();

        this.packFile = new(() =>
        {
            // On 64-bit processes, we can use memory mapped streams (the address space
            // will be large enough to map the entire pack file). On 32-bit processes,
            // we directly access the underlying stream.
            if (IntPtr.Size <= 4)
            {
                return null;
            }

            var file = MemoryMappedFile.CreateFromFile(this.packStream(), mapName: null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);
            return (file, file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read));
        });
    }

    /// <summary>
    /// Resolves a Git object across the whole object store. Used to resolve the base
    /// object of <c>ref-delta</c> deltified objects.
    /// </summary>
    /// <param name="objectId">The Git object id of the object to fetch.</param>
    /// <param name="objectType">The expected object type, or <see langword="null"/> to accept any type.</param>
    /// <returns>A stream over the object data and its actual object type, or <see langword="null"/> if not found.</returns>
    public (Stream Stream, string ObjectType)? ResolveBaseObject(GitObjectId objectId, string? objectType) =>
        this.resolveGitObject(objectId, objectType);

    /// <summary>
    /// Attempts to retrieve a Git object from this Git pack.
    /// </summary>
    /// <param name="objectId">The Git object id of the object to retrieve.</param>
    /// <param name="objectType">The expected object type, or <see langword="null"/> to accept any type.</param>
    /// <param name="stream">If found, receives a <see cref="Stream"/> which represents the object.</param>
    /// <param name="actualType">If found, receives the actual object type.</param>
    /// <returns><see langword="true"/> if the object was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetObject(GitObjectId objectId, string? objectType, [NotNullWhen(true)] out Stream? stream, [NotNullWhen(true)] out string? actualType)
    {
        var offset = GetOffset(objectId);

        if (offset is null)
        {
            stream = null;
            actualType = null;
            return false;
        }

        try
        {
            (stream, actualType) = GetObject(offset.Value, objectType);
            return true;
        }
        catch (GitObjectStoreException exception) when (exception.ObjectNotFound)
        {
            stream = null;
            actualType = null;
            return false;
        }
    }

    /// <summary>
    /// Gets a Git object at a specific offset.
    /// </summary>
    /// <param name="offset">The offset of the Git object, relative to the pack file.</param>
    /// <param name="objectType">The expected object type, or <see langword="null"/> to accept any type.</param>
    /// <returns>A <see cref="Stream"/> which represents the object, and the actual object type.</returns>
    public (Stream Stream, string ObjectType) GetObject(long offset, string? objectType)
    {
        if (this.cache.TryOpen(offset, out var cachedStream, out var cachedType))
        {
            if (objectType is not null && cachedType != objectType)
            {
                cachedStream.Dispose();
                throw new GitObjectStoreException($"An object of type {objectType} could not be located at offset {offset}.") { ObjectNotFound = true };
            }

            return (cachedStream, cachedType);
        }

        var packStream = GetPackStream();
        Stream objectStream;
        string actualType;

        try
        {
            (objectStream, actualType) = GitPackReader.GetObject(this, packStream, offset, objectType);
        }
        catch
        {
            packStream.Dispose();
            throw;
        }

        return (this.cache.Add(offset, objectStream, actualType), actualType);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.indexReader.IsValueCreated)
        {
            this.indexReader.Value.Dispose();
        }

        if (this.packFile.IsValueCreated && this.packFile.Value is { } mapped)
        {
            mapped.Accessor.Dispose();
            mapped.File.Dispose();
        }

        this.cache.Dispose();
    }

    private long? GetOffset(GitObjectId objectId)
    {
        if (this.offsets.TryGetValue(objectId, out var cachedOffset))
        {
            return cachedOffset;
        }

        var offset = this.indexReader.Value.GetOffset(objectId);

        if (offset is not null)
        {
            this.offsets.Add(objectId, offset.Value);
        }

        return offset;
    }

    private Stream GetPackStream() =>
        this.packFile.Value is { } mapped
            ? new MemoryMappedStream(mapped.Accessor)
            : this.packStream();
}
