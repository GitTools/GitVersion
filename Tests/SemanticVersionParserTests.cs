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
        Assert.AreEqual(3, version.Patch);
        Assert.AreEqual(Stability.Final, version.Stability);
    }
    [Test]
    public void Major_minor()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(0,version.Patch);
        Assert.AreEqual(Stability.Final,version.Stability);
    }
    [Test]
    public void Major()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(0,version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stability.Final, version.Stability);
    }

    [Test]
    public void Major_minor_patch_stability_beta()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3-beta3", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.Beta,version.Stability);
        Assert.AreEqual(3,version.PreReleasePartOne);
    }
    [Test]
    public void Major_minor_patch_stability_Alpha()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3-alpha4", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.Alpha,version.Stability);
        Assert.AreEqual(4,version.PreReleasePartOne);
    }
    [Test]
    public void Major_minor_patch_lower_stability()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3-alpha4", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.Alpha,version.Stability);
        Assert.AreEqual(4,version.PreReleasePartOne);
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
        Assert.AreEqual(3,version.PreReleasePartOne);
    }
    [Test]
    public void BothPreRelease()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3-rc3.1", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.ReleaseCandidate,version.Stability);
        Assert.AreEqual(3,version.PreReleasePartOne);
        Assert.AreEqual(1,version.PreReleasePartTwo);
    }
    [Test]
    public void Major_minor_patch_rc_upper_stability()
    {
        SemanticVersion version;
        SemanticVersionParser.TryParse("1.2.3-RC3", out version);
        Assert.AreEqual(1,version.Major);
        Assert.AreEqual(2,version.Minor);
        Assert.AreEqual(3,version.Patch);
        Assert.AreEqual(Stability.ReleaseCandidate,version.Stability);
        Assert.AreEqual(3,version.PreReleasePartOne);
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
        Assert.AreEqual(3,version.PreReleasePartOne);
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