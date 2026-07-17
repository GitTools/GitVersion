// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.IO.Compression;

namespace GitVersion.Git;

/// <summary>
/// A <see cref="Stream"/> which reads zlib-compressed data.
/// </summary>
/// <remarks>
/// <para>
/// This stream parses but ignores the two-byte zlib header at the start of the compressed
/// stream. It keeps track of the current position and, if provided, the length, and supports
/// forward-only seeking.
/// </para>
/// <para>
/// This class wraps a <see cref="DeflateStream"/> rather than inheriting from it, because
/// <see cref="DeflateStream"/> detects whether <c>Read(Span{byte})</c> is being overridden
/// and behaves differently when it is.
/// </para>
/// </remarks>
internal class GitZLibStream : Stream
{
    private readonly DeflateStream stream;
    private long length;
    private long position;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitZLibStream"/> class.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> from which to read data.</param>
    /// <param name="length">The size of the uncompressed data.</param>
    public GitZLibStream(Stream stream, long length = -1)
    {
        this.stream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: false);
        this.length = length;

        Span<byte> zlibHeader = stackalloc byte[2];
        stream.ReadExactly(zlibHeader);

        if (zlibHeader[0] != 0x78 || (zlibHeader[1] != 0x01 && zlibHeader[1] != 0x9C && zlibHeader[1] != 0x5E && zlibHeader[1] != 0xDA))
        {
            throw new GitObjectStoreException($"Invalid zlib header {zlibHeader[0]:X2} {zlibHeader[1]:X2}");
        }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get => this.position;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override long Length => this.length;

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = this.stream.Read(buffer, offset, count);
        this.position += read;
        return read;
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        var read = this.stream.Read(buffer);
        this.position += read;
        return read;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        var value = this.stream.ReadByte();

        if (value != -1)
        {
            this.position += 1;
        }

        return value;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin && offset == this.position)
        {
            return this.position;
        }

        if (origin == SeekOrigin.Current && offset == 0)
        {
            return this.position;
        }

        if (origin == SeekOrigin.Begin && offset > this.position)
        {
            this.ReadBytes(checked((int)(offset - this.position)));
            return this.position;
        }

        throw new NotSupportedException("Only forward seeks are supported.");
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

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
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Initializes the length and position properties.
    /// </summary>
    /// <param name="length">The length of the uncompressed data.</param>
    protected void Initialize(long length)
    {
        this.position = 0;
        this.length = length;
    }
}
