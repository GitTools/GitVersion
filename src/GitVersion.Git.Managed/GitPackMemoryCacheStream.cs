// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// A stream which caches the data read from an underlying (non-seekable) stream in memory,
/// providing random read-only access to that data.
/// </summary>
internal sealed class GitPackMemoryCacheStream : Stream
{
    private readonly Stream stream;
    private readonly MemoryStream cacheStream = new();
    private readonly long length;

    public GitPackMemoryCacheStream(Stream stream)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.length = this.stream.Length;
    }

    /// <summary>
    /// Gets the object on which <see cref="GitPackMemoryCacheViewStream"/> instances synchronize
    /// their access to this shared stream.
    /// </summary>
    public object SyncRoot { get; } = new();

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => this.length;

    /// <inheritdoc/>
    public override long Position
    {
        get => this.cacheStream.Position;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        if (this.cacheStream.Length < this.length
            && this.cacheStream.Position + buffer.Length > this.cacheStream.Length)
        {
            var currentPosition = this.cacheStream.Position;
            var toRead = (int)(buffer.Length - this.cacheStream.Length + this.cacheStream.Position);
            var actualRead = this.stream.Read(buffer[..toRead]);
            this.cacheStream.Seek(0, SeekOrigin.End);
            this.cacheStream.Write(buffer[..actualRead]);
            this.cacheStream.Seek(currentPosition, SeekOrigin.Begin);
            DisposeStreamIfRead();
        }

        return this.cacheStream.Read(buffer);
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin != SeekOrigin.Begin)
        {
            throw new NotSupportedException();
        }

        if (offset > this.cacheStream.Length)
        {
            this.cacheStream.Seek(0, SeekOrigin.End);
            var toRead = (int)(offset - this.cacheStream.Length);
            this.stream.ReadBytes(toRead, this.cacheStream);
            DisposeStreamIfRead();
            return this.cacheStream.Position;
        }

        return this.cacheStream.Seek(offset, origin);
    }

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.stream.Dispose();
            this.cacheStream.Dispose();
        }

        base.Dispose(disposing);
    }

    private void DisposeStreamIfRead()
    {
        if (this.cacheStream.Length == this.stream.Length)
        {
            this.stream.Dispose();
        }
    }
}
