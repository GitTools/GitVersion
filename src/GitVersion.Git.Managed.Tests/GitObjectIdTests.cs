namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitObjectIdTests
{
    private const string Sha1Hex = "4e912736c27e40b389904d046dc63dc9f578117f";
    private const string Sha256Hex = "9c5e01e5827b7a2e0f9b2ecabf5ee89f4fd8b7c153bf8dd9b1f38f9b7d6f2a3c";

    [Test]
    public void ParsesA40CharacterSha1HexString()
    {
        var objectId = GitObjectId.Parse(Sha1Hex);

        objectId.HashLength.ShouldBe(GitObjectId.Sha1Size);
        objectId.ToString().ShouldBe(Sha1Hex);
    }

    [Test]
    public void ParsesUpperCaseHexAndNormalizesToLowerCase()
    {
        var objectId = GitObjectId.Parse(Sha1Hex.ToUpperInvariant().AsSpan());

        objectId.ToString().ShouldBe(Sha1Hex);
    }

    [Test]
    public void ParsesA64CharacterSha256HexString()
    {
        var objectId = GitObjectId.Parse(Sha256Hex);

        objectId.HashLength.ShouldBe(GitObjectId.Sha256Size);
        objectId.ToString().ShouldBe(Sha256Hex);
    }

    [Test]
    public void ParsesRawBytes()
    {
        var bytes = Convert.FromHexString(Sha1Hex);

        var objectId = GitObjectId.Parse(bytes);

        objectId.HashLength.ShouldBe(GitObjectId.Sha1Size);
        objectId.ToString().ShouldBe(Sha1Hex);
    }

    [Test]
    public void ParsesAsciiEncodedHex()
    {
        var asciiHex = Encoding.ASCII.GetBytes(Sha1Hex);

        var objectId = GitObjectId.ParseHex(asciiHex);

        objectId.ToString().ShouldBe(Sha1Hex);
    }

    [TestCase(0)]
    [TestCase(19)]
    [TestCase(39)]
    [TestCase(41)]
    public void ParseRejectsInvalidHexLengths(int length) =>
        Should.Throw<ArgumentException>(() => GitObjectId.Parse(new string('a', length)));

    [Test]
    public void ParseRejectsInvalidRawByteLengths() =>
        Should.Throw<ArgumentException>(() => GitObjectId.Parse(new byte[21]));

    [Test]
    public void ParseRejectsNonHexCharacters() =>
        Should.Throw<FormatException>(() => GitObjectId.Parse("zz12736c27e40b389904d046dc63dc9f578117f4"));

    [Test]
    public void EqualObjectIdsAreEqualAndHaveTheSameHashCode()
    {
        var left = GitObjectId.Parse(Sha1Hex);
        var right = GitObjectId.ParseHex(Encoding.ASCII.GetBytes(Sha1Hex));

        left.ShouldBe(right);
        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.Equals((object)right).ShouldBeTrue();
    }

    [Test]
    public void DifferentObjectIdsAreNotEqual()
    {
        var left = GitObjectId.Parse(Sha1Hex);
        var right = GitObjectId.Parse("4e912736c27e40b389904d046dc63dc9f5781180");

        left.ShouldNotBe(right);
        (left == right).ShouldBeFalse();
        (left != right).ShouldBeTrue();
    }

    [Test]
    public void ASha1IdIsNotEqualToASha256IdWithTheSamePrefix()
    {
        var sha1 = GitObjectId.Parse(Sha1Hex);
        var sha256 = GitObjectId.Parse(Sha1Hex + new string('0', 24));

        sha1.ShouldNotBe(sha256);
    }

    [Test]
    public void CanBeUsedAsADictionaryKey()
    {
        var dictionary = new Dictionary<GitObjectId, string>
        {
            [GitObjectId.Parse(Sha1Hex)] = "value"
        };

        dictionary[GitObjectId.ParseHex(Encoding.ASCII.GetBytes(Sha1Hex))].ShouldBe("value");
    }

    [TestCase(0, "")]
    [TestCase(1, "4")]
    [TestCase(7, "4e91273")]
    [TestCase(40, Sha1Hex)]
    public void ToStringWithLengthReturnsAHexPrefix(int length, string expected)
    {
        var objectId = GitObjectId.Parse(Sha1Hex);

        objectId.ToString(length).ShouldBe(expected);
    }

    [Test]
    public void ToStringWithInvalidLengthThrows()
    {
        var objectId = GitObjectId.Parse(Sha1Hex);

        Should.Throw<ArgumentOutOfRangeException>(() => objectId.ToString(41));
        Should.Throw<ArgumentOutOfRangeException>(() => objectId.ToString(-1));
    }

    [Test]
    public void CopyToCopiesTheRawBytes()
    {
        var objectId = GitObjectId.Parse(Sha1Hex);
        var buffer = new byte[GitObjectId.Sha1Size];

        objectId.CopyTo(buffer);

        buffer.ShouldBe(Convert.FromHexString(Sha1Hex));
    }

    [Test]
    public void AsUInt16ReturnsTheFirstTwoBytes()
    {
        var objectId = GitObjectId.Parse(Sha1Hex);

        objectId.AsUInt16().ShouldBe((ushort)0x4e91);
    }

    [Test]
    public void EmptyHasAZeroHashLength()
    {
        GitObjectId.Empty.HashLength.ShouldBe(0);
        GitObjectId.Empty.ShouldBe(default(GitObjectId));
    }
}
