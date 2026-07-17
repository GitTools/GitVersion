// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Git;

/// <summary>
/// Represents a cache in which objects retrieved from a <see cref="GitPack"/> are cached.
/// Caching these objects can be of interest, because retrieving data from a <see cref="GitPack"/>
/// can be potentially expensive: the data is compressed and can be deltified.
/// </summary>
internal abstract class GitPackCache : IDisposable
{
    /// <summary>
    /// Attempts to retrieve a Git object from cache.
    /// </summary>
    /// <param name="offset">The offset of the Git object in the Git pack.</param>
    /// <param name="stream">A <see cref="Stream"/> which will be set to the cached data, if found.</param>
    /// <param name="objectType">The object type of the cached object, if found.</param>
    /// <returns><see langword="true"/> if the object was found in cache; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryOpen(long offset, [NotNullWhen(true)] out Stream? stream, [NotNullWhen(true)] out string? objectType);

    /// <summary>
    /// Adds a Git object to this cache.
    /// </summary>
    /// <param name="offset">The offset of the Git object in the Git pack.</param>
    /// <param name="stream">A <see cref="Stream"/> which represents the object to add.</param>
    /// <param name="objectType">The object type of the object to add to the cache.</param>
    /// <returns>A <see cref="Stream"/> which represents the cached entry.</returns>
    public abstract Stream Add(long offset, Stream stream, string objectType);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of native and managed resources associated by this object.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed and native resources; <see langword="false"/> to only dispose of native resources.</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}
