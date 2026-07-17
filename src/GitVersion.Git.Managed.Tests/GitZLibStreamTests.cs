namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitZLibStreamTests
{
    [Test]
    public void DecompressesAStreamWithASmallerWindowSize()
    {
        // RFC 1950 CMF 0x18 selects deflate with a 512-byte window.
        var compressed = Convert.FromHexString("18954BCF2C294B2D2ACECCCF53C84DCC4B4C4F4D51A8CAC94C02006F440909");
        using var stream = new GitZLibStream(new MemoryStream(compressed));
        using var reader = new StreamReader(stream);

        reader.ReadToEnd().ShouldBe("gitversion managed zlib");
    }

    [TestCase("7800")]
    [TestCase("7820")]
    [TestCase("881C")]
    public void RejectsInvalidOrUnsupportedHeaders(string header)
    {
        using var compressed = new MemoryStream(Convert.FromHexString(header));

        Should.Throw<GitObjectStoreException>(() => new GitZLibStream(compressed));
    }
}
