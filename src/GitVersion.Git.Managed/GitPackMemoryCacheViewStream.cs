// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// A read-only view over a shared <see cref="GitPackMemoryCacheStream"/> which maintains
/// its own position, so multiple readers can independently consume the same cached object.
/// </summary>
internal sealed class GitPackMemoryCacheViewStream : Stream
{
    private readonly GitPackMemoryCacheStream baseStream;

    private long position;

    public GitPackMemoryCacheViewStream(GitPackMemoryCacheStream baseStream) =>
        this.baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => this.baseStream.Length;

    /// <inheritdoc/>
    public override long Position
    {
        get => this.position;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        int read;

        lock (this.baseStream)
        {
            if (this.baseStream.Position != this.position)
            {
                this.baseStream.Seek(this.position, SeekOrigin.Begin);
            }

            read = this.baseStream.Read(buffer);
        }

        this.position += read;
        return read;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin != SeekOrigin.Begin)
        {
            throw new NotSupportedException();
        }

        this.position = Math.Min(offset, Length);
        return this.position;
    }

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
