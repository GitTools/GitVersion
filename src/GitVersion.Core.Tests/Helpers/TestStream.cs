namespace GitVersion.Core.Tests.Helpers;

public class TestStream(string path, IFileSystem testFileSystem) : Stream
{
    private readonly MemoryStream underlying = new();

    protected override void Dispose(bool disposing)
    {
        Flush();
        base.Dispose(disposing);
    }

    public override void Flush()
    {
        this.underlying.Position = 0;
        var readToEnd = new StreamReader(this.underlying).ReadToEnd();
        testFileSystem.WriteAllText(path, readToEnd);
    }

    public override long Seek(long offset, SeekOrigin origin) => this.underlying.Seek(offset, origin);

    public override void SetLength(long value) => this.underlying.SetLength(value);

    public override int Read(byte[] buffer, int offset, int count) => this.underlying.Read(buffer, offset, count);

    public override void Write(byte[] buffer, int offset, int count) => this.underlying.Write(buffer, offset, count);

    public override bool CanRead => this.underlying.CanRead;
    public override bool CanSeek => this.underlying.CanSeek;
    public override bool CanWrite => this.underlying.CanWrite;
    public override long Length => this.underlying.Length;

    public override long Position
    {
        get => this.underlying.Position;
        set => this.underlying.Position = value;
    }
}
