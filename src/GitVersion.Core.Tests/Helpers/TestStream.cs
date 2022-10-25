namespace GitVersion.Core.Tests.Helpers;

public class TestStream : Stream
{
    private readonly string path;
    private readonly TestFileSystem testFileSystem;
    private readonly MemoryStream underlying = new();

    public TestStream(string path, TestFileSystem testFileSystem)
    {
        this.path = path;
        this.testFileSystem = testFileSystem;
    }

    protected override void Dispose(bool disposing)
    {
        Flush();
        base.Dispose(disposing);
    }

    public override void Flush()
    {
        this.underlying.Position = 0;
        var readToEnd = new StreamReader(this.underlying).ReadToEnd();
        this.testFileSystem.WriteAllText(this.path, readToEnd);
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
