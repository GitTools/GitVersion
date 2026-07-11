// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using Microsoft.Win32.SafeHandles;

namespace GitVersion.Git;

/// <summary>
/// Provides read-only, seekable access to a file through a shared <see cref="SafeFileHandle"/>.
/// Each instance maintains its own position, so multiple streams can read the same file
/// concurrently without interfering with each other. The handle is not owned by this stream.
/// </summary>
internal sealed class RandomAccessStream : Stream
{
    private readonly SafeFileHandle handle;
    private readonly long length;
    private long position;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomAccessStream"/> class.
    /// </summary>
    /// <param name="handle">The handle of the file to read. Remains owned by the caller.</param>
    /// <param name="length">The length of the file.</param>
    public RandomAccessStream(SafeFileHandle handle, long length)
    {
        this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
        this.length = length;
    }

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
        get => this.position;
        set => this.position = value;
    }

    /// <inheritdoc/>
    public override void Flush()
    {
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        var toRead = (int)Math.Min(buffer.Length, this.length - this.position);
        if (toRead <= 0)
        {
            return 0;
        }

        var read = RandomAccess.Read(this.handle, buffer[..toRead], this.position);
        this.position += read;
        return read;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.position + offset,
            _ => throw new NotSupportedException()
        };

        if (newPosition > this.length)
        {
            newPosition = this.length;
        }

        if (newPosition < 0)
        {
            throw new IOException("Attempted to seek before the start or beyond the end of the stream.");
        }

        this.position = newPosition;
        return this.position;
    }

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
