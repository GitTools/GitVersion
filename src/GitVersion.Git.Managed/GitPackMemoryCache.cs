// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Git;

/// <summary>
/// <para>
/// When a <see cref="Stream"/> is added to the <see cref="GitPackMemoryCache"/>, it is wrapped
/// in a <see cref="GitPackMemoryCacheStream"/>. This stream allows for just-in-time, random,
/// read-only access to the underlying data (which may be deltified and/or compressed).
/// </para>
/// <para>
/// Whenever data is read from a <see cref="GitPackMemoryCacheStream"/>, the call is forwarded to the
/// underlying <see cref="Stream"/> and cached in a <see cref="MemoryStream"/>. If the same data is read
/// twice, it is read from the <see cref="MemoryStream"/>, rather than the underlying <see cref="Stream"/>.
/// </para>
/// </summary>
internal sealed class GitPackMemoryCache : GitPackCache
{
    private readonly Dictionary<long, (GitPackMemoryCacheStream Stream, string ObjectType)> cache = [];

    /// <inheritdoc/>
    public override Stream Add(long offset, Stream stream, string objectType)
    {
        var cacheStream = new GitPackMemoryCacheStream(stream);
        this.cache.Add(offset, (cacheStream, objectType));
        return new GitPackMemoryCacheViewStream(cacheStream);
    }

    /// <inheritdoc/>
    public override bool TryOpen(long offset, [NotNullWhen(true)] out Stream? stream, [NotNullWhen(true)] out string? objectType)
    {
        if (this.cache.TryGetValue(offset, out var entry))
        {
            stream = new GitPackMemoryCacheViewStream(entry.Stream);
            objectType = entry.ObjectType;
            return true;
        }

        stream = null;
        objectType = null;
        return false;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var (stream, _) in this.cache.Values)
            {
                stream.Dispose();
            }

            this.cache.Clear();
        }

        base.Dispose(disposing);
    }
}
