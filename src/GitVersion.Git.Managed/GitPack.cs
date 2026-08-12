// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Diagnostics.CodeAnalysis;

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
    private readonly Lazy<FileStream> packStream;
    private readonly GitPackCache cache;

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
        this.indexReader = new(() => new GitPackIndexReader(File.OpenRead(indexPath)));
        this.packStream = new(() => File.OpenRead(packPath));
        this.cache = cache ?? new GitPackMemoryCache();
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

        var packDataStream = GetPackStream();
        Stream objectStream;
        string actualType;

        try
        {
            (objectStream, actualType) = GitPackReader.GetObject(this, packDataStream, offset, objectType);
        }
        catch
        {
            packDataStream.Dispose();
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

        if (this.packStream.IsValueCreated)
        {
            this.packStream.Value.Dispose();
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

    private Stream GetPackStream()
    {
        var file = this.packStream.Value;
        return new RandomAccessStream(file.SafeFileHandle, file.Length);
    }
}
