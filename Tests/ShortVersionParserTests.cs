using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class ShortVersionParserTests
{

    [Test]
    public void Major_minor_patch()
    {
        int minor;
        int major;
        int patch;
        ShortVersionParser.Parse("1.2.3", out major,out minor,out patch);
        Assert.AreEqual(1, major);
        Assert.AreEqual(2, minor);
        Assert.AreEqual(3, patch);
    }

    [Test]
    public void Major_minor_patchTry()
    {
        int minor;
        int major;
        int patch;
        var result = ShortVersionParser.TryParse("1.2.3", out major,out minor,out patch);
        Assert.IsTrue(result);
        Assert.AreEqual(1, major);
        Assert.AreEqual(2, minor);
        Assert.AreEqual(3, patch);
    }

    [Test]
    public void Major_minorTry()
    {
        int minor;
        int major;
        var result = ShortVersionParser.TryParseMajorMinor("1.2.3", out major, out minor);
        Assert.IsFalse(result);

        result = ShortVersionParser.TryParseMajorMinor("1.2", out major, out minor);
        Assert.IsFalse(result);

        result = ShortVersionParser.TryParseMajorMinor("1.2.0", out major, out minor);
        Assert.IsTrue(result);

        Assert.AreEqual(1, major);
        Assert.AreEqual(2, minor);
    }
}
