using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class SemanticVersionParserTests
{

    [Test]
    public void Major_minor_patch()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
    }
    [Test]
    public void Major_minor()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(0,version.Patch);
    }
    [Test]
    public void Major()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(0,version.Minor);
        Assert.AreEqual(0,version.Patch);
    }

    [Test]
    public void Major_minor_patch_stability()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3-beta3", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.Beta,version.Stability);
        Assert.AreEqual(3,version.PreReleaseNumber);
    }
    [Test]
    public void Major_minor_patch_rc_stability()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3-rc3", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.ReleaseCandidate,version.Stability);
        Assert.AreEqual(3,version.PreReleaseNumber);
    }

    [Test]
    public void Major_minor_patch_stability_padded_zeroes()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("01.02.03-rc03", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.ReleaseCandidate,version.Stability);
        Assert.AreEqual(3,version.PreReleaseNumber);
    }

    [Test]
    public void Major_minor_patch_stability_missing_pre_release()
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersionParser.TryParse("1.2.3-beta", out version));
    }
    [Test]
    public void Trailing_character()
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersionParser.TryParse("1.2.3-beta3f", out version));
    }

    [Test]
    public void Bad_stability()
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersionParser.TryParse("1.2.3-notAStability1", out version));
    }

    [Test]
    public void PlainText()
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersionParser.TryParse("someText", out version));
    }

    [Test]
    public void Too_many_main_version_parts()
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersionParser.TryParse("1.2.3.4", out version));
    }

    [Test]
    public void Too_many_parts()
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersionParser.TryParse("some-T-ext", out version));
    }


}