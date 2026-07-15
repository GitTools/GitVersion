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
/// <para>
/// The cache is bounded: when the decompressed size of the retained entries exceeds the budget,
/// the least recently used entries are evicted (and disposed once no view is reading them),
/// so full-history walks over large repositories do not retain every object in memory.
/// </para>
/// </summary>
internal sealed class GitPackMemoryCache : GitPackCache
{
    // libgit2 caps its object cache at 256 MB by default; the same budget keeps the
    // hot delta-base entries while releasing objects no walk will read again.
    private const long MaxTotalSize = 256 * 1024 * 1024;

    private readonly object syncRoot = new();
    private readonly Dictionary<long, LinkedListNode<CacheEntry>> cache = [];
    private readonly LinkedList<CacheEntry> recency = new();
    private long totalSize;

    private sealed record CacheEntry(long Offset, GitPackMemoryCacheStream Stream, string ObjectType);

    /// <inheritdoc/>
    public override Stream Add(long offset, Stream stream, string objectType)
    {
        var cacheStream = new GitPackMemoryCacheStream(stream);
        var view = new GitPackMemoryCacheViewStream(cacheStream);

        lock (this.syncRoot)
        {
            if (this.cache.ContainsKey(offset))
            {
                // Someone cached this offset between the caller's TryOpen and this Add.
                // Keep the existing entry; the returned view keeps its own stream alive.
                cacheStream.Release();
                return view;
            }

            var node = this.recency.AddFirst(new CacheEntry(offset, cacheStream, objectType));
            this.cache.Add(offset, node);
            this.totalSize += cacheStream.Length;
            EvictWhileOverBudget();
        }

        return view;
    }

    /// <inheritdoc/>
    public override bool TryOpen(long offset, [NotNullWhen(true)] out Stream? stream, [NotNullWhen(true)] out string? objectType)
    {
        lock (this.syncRoot)
        {
            if (this.cache.TryGetValue(offset, out var node))
            {
                this.recency.Remove(node);
                this.recency.AddFirst(node);
                stream = new GitPackMemoryCacheViewStream(node.Value.Stream);
                objectType = node.Value.ObjectType;
                return true;
            }
        }

        stream = null;
        objectType = null;
        return false;
    }

    private void EvictWhileOverBudget()
    {
        // Never evict the most recent entry: a single object larger than the whole
        // budget must still be readable through the entry just added.
        while (this.totalSize > MaxTotalSize && this.recency.Count > 1 && this.recency.Last is { } tail)
        {
            this.recency.RemoveLast();
            this.cache.Remove(tail.Value.Offset);
            this.totalSize -= tail.Value.Stream.Length;
            tail.Value.Stream.Release();
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (this.syncRoot)
            {
                foreach (var entry in this.recency)
                {
                    entry.Stream.Release();
                }

                this.recency.Clear();
                this.cache.Clear();
                this.totalSize = 0;
            }
        }

        base.Dispose(disposing);
    }
}
