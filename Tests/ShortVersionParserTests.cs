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
    public void Major_minor_missingPatch()
    {
        int minor;
        int major;
        int patch;
        var result = ShortVersionParser.TryParse("1.2", out major, out minor, out patch);

        Assert.IsTrue(result);
        Assert.AreEqual(1, major);
        Assert.AreEqual(2, minor);
        Assert.AreEqual(0, patch);
    }
}