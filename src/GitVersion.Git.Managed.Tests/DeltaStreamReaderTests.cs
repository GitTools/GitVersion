namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class DeltaStreamReaderTests
{
    [Test]
    public void ReadsACompleteCopyInstruction()
    {
        // Copy with one offset byte (0x42) and one size byte (0x07).
        using var stream = new MemoryStream([0b1001_0001, 0x42, 0x07]);

        var instruction = DeltaStreamReader.Read(stream);

        instruction.ShouldNotBeNull();
        instruction.Value.InstructionType.ShouldBe(DeltaInstructionType.Copy);
        instruction.Value.Offset.ShouldBe(0x42);
        instruction.Value.Size.ShouldBe(0x07);
    }

    [Test]
    public void ReturnsNullAtTheEndOfTheStream()
    {
        using var stream = new MemoryStream([]);

        DeltaStreamReader.Read(stream).ShouldBeNull();
    }

    [Test]
    public void ThrowsWhenTheStreamEndsInsideACopyInstruction()
    {
        // The instruction announces an offset byte and a size byte, but the stream is
        // truncated: EOF must not be silently decoded as 0xFF (garbage copy target).
        using var stream = new MemoryStream([0b1001_0001, 0x42]);

        Should.Throw<EndOfStreamException>(() => DeltaStreamReader.Read(stream));
    }
}
