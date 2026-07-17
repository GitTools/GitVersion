// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Reads data from a deltified object.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format#_deltified_representation"/>
internal sealed class GitPackDeltafiedStream : Stream
{
    private readonly long length;

    private readonly Stream baseStream;
    private readonly Stream deltaStream;

    private long position;
    private DeltaInstruction? current;
    private int offset;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitPackDeltafiedStream"/> class.
    /// </summary>
    /// <param name="baseStream">The base stream to which the deltas are applied.</param>
    /// <param name="deltaStream">A <see cref="Stream"/> which contains a sequence of <see cref="DeltaInstruction"/>s.</param>
    public GitPackDeltafiedStream(Stream baseStream, Stream deltaStream)
    {
        this.baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        this.deltaStream = deltaStream ?? throw new ArgumentNullException(nameof(deltaStream));

        _ = this.deltaStream.ReadMbsInt(); // base object length
        this.length = this.deltaStream.ReadMbsInt();
    }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => this.length;

    /// <inheritdoc/>
    public override long Position
    {
        get => this.position;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        var read = 0;

        while (read < buffer.Length && TryGetInstruction(out var instruction))
        {
            var source = instruction.InstructionType == DeltaInstructionType.Copy ? this.baseStream : this.deltaStream;

            var canRead = Math.Min(buffer.Length - read, instruction.Size - this.offset);
            var didRead = source.Read(buffer.Slice(read, canRead));

            if (didRead == 0)
            {
                throw new EndOfStreamException();
            }

            read += didRead;
            this.offset += didRead;
        }

        this.position += read;
        return read;
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

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
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.deltaStream.Dispose();
            this.baseStream.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool TryGetInstruction(out DeltaInstruction instruction)
    {
        if (this.current is not null && this.offset < this.current.Value.Size)
        {
            instruction = this.current.Value;
            return true;
        }

        this.current = DeltaStreamReader.Read(this.deltaStream);

        if (this.current is null)
        {
            instruction = default;
            return false;
        }

        instruction = this.current.Value;

        switch (instruction.InstructionType)
        {
            case DeltaInstructionType.Copy:
                this.baseStream.Seek(instruction.Offset, SeekOrigin.Begin);
                this.offset = 0;
                break;

            case DeltaInstructionType.Insert:
                this.offset = 0;
                break;

            default:
                throw new GitObjectStoreException($"Invalid delta instruction type '{instruction.InstructionType}'.");
        }

        return true;
    }
}
