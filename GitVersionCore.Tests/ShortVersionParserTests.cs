using GitVersion;
using NUnit.Framework;

[TestFixture]
public class ShortVersionParserTests
{

    [Test]
    public void Major_minor_patch()
    {
        var shortVersion = ShortVersionParser.Parse("1.2.3");
        Assert.AreEqual(1, shortVersion.Major);
        Assert.AreEqual(2, shortVersion.Minor);
        Assert.AreEqual(3, shortVersion.Patch);
    }
    [Test]
    public void With_V()
    {
        var shortVersion = ShortVersionParser.Parse("V1.2.3");
        Assert.AreEqual(1, shortVersion.Major);
        Assert.AreEqual(2, shortVersion.Minor);
        Assert.AreEqual(3, shortVersion.Patch);
    }
    [Test]
    public void With_v()
    {
        var shortVersion = ShortVersionParser.Parse("v1.2.3");
        Assert.AreEqual(1, shortVersion.Major);
        Assert.AreEqual(2, shortVersion.Minor);
        Assert.AreEqual(3, shortVersion.Patch);
    }

    [Test]
    public void Major_minor_patchTry()
    {
        ShortVersion shortVersion;
        var result = ShortVersionParser.TryParse("1.2.3", out shortVersion);
        Assert.IsTrue(result);
        Assert.AreEqual(1, shortVersion.Major);
        Assert.AreEqual(2, shortVersion.Minor);
        Assert.AreEqual(3, shortVersion.Patch);
    }

    [Test]
    public void Major_minor_missingPatch()
    {
        ShortVersion shortVersion;
        var result = ShortVersionParser.TryParse("1.2", out shortVersion);

        Assert.IsTrue(result);
        Assert.AreEqual(1, shortVersion.Major);
        Assert.AreEqual(2, shortVersion.Minor);
        Assert.AreEqual(0, shortVersion.Patch);
    }

    [Test]
    public void Major_minorTry()
    {
        ShortVersion shortVersion;
        var result = ShortVersionParser.TryParseMajorMinor("1.2.3", out shortVersion);
        Assert.IsFalse(result);

        result = ShortVersionParser.TryParseMajorMinor("1.2.0-alpha1", out shortVersion);
        Assert.IsFalse(result);

        result = ShortVersionParser.TryParseMajorMinor("1.2.0.0", out shortVersion);
        Assert.IsFalse(result);

        result = ShortVersionParser.TryParseMajorMinor("1.2.0.1", out shortVersion);
        Assert.IsFalse(result);

        result = ShortVersionParser.TryParseMajorMinor("1.2", out shortVersion);
        Assert.IsTrue(result);

        result = ShortVersionParser.TryParseMajorMinor("1.2.0", out shortVersion);
        Assert.IsTrue(result);

        Assert.AreEqual(1, shortVersion.Major);
        Assert.AreEqual(2, shortVersion.Minor);
    }
}
