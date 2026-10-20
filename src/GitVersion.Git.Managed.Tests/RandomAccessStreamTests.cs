namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class RandomAccessStreamTests
{
    [Test]
    public void ReadsUnsignedBytesAndAdvancesPositionAlongsideSpanReads()
    {
        using var file = CreateFile([0, 128, 255]);
        using var stream = new RandomAccessStream(file.SafeFileHandle, file.Length);

        stream.ReadByte().ShouldBe(0);
        stream.Position.ShouldBe(1);
        Span<byte> buffer = stackalloc byte[1];
        stream.Read(buffer).ShouldBe(1);
        buffer[0].ShouldBe((byte)128);
        stream.ReadByte().ShouldBe(255);
        stream.Position.ShouldBe(3);
    }

    [TestCase(0)]
    [TestCase(2)]
    public void ReturnsEndOfStreamAtTheDeclaredLengthWithoutAdvancing(int length)
    {
        using var file = CreateFile([1, 2, 3]);
        using var stream = new RandomAccessStream(file.SafeFileHandle, length);
        stream.Position = length;

        stream.ReadByte().ShouldBe(-1);
        stream.ReadByte().ShouldBe(-1);
        stream.Position.ShouldBe(length);
    }

    [Test]
    public void ReturnsEndOfStreamWhenTheFileIsShorterThanTheDeclaredLength()
    {
        using var file = CreateFile([42]);
        using var stream = new RandomAccessStream(file.SafeFileHandle, 2);

        stream.ReadByte().ShouldBe(42);
        stream.ReadByte().ShouldBe(-1);
        stream.Position.ShouldBe(1);
    }

    [Test]
    public void KeepsPositionsIndependentAndLeavesTheSharedHandleOpen()
    {
        using var file = CreateFile([10, 20, 30]);
        using var stream = new RandomAccessStream(file.SafeFileHandle, file.Length);
        using (var other = new RandomAccessStream(file.SafeFileHandle, file.Length))
        {
            stream.Seek(2, SeekOrigin.Begin);
            stream.ReadByte().ShouldBe(30);
            other.ReadByte().ShouldBe(10);
            other.Position.ShouldBe(1);
            stream.Seek(1, SeekOrigin.Begin);
        }

        stream.ReadByte().ShouldBe(20);
        stream.Position.ShouldBe(2);
    }

    [Test]
    public void ReadsBytesWithoutAllocatingPerCall()
    {
        using var file = CreateFile(new byte[2048]);
        using var stream = new RandomAccessStream(file.SafeFileHandle, file.Length);
        for (var i = 0; i < 1024; i++)
        {
            stream.ReadByte();
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++)
        {
            stream.ReadByte();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        allocated.ShouldBe(0);
        stream.Position.ShouldBe(2048);
    }

    private static FileStream CreateFile(byte[] contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"gitversion-stream-{Guid.NewGuid():N}");
        var file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
        file.Write(contents);
        file.Flush();
        return file;
    }
}
