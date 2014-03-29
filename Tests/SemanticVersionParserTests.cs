using GitVersion;
using NUnit.Framework;

[TestFixture]
public class SemanticVersionParserTests
{

    [TestCase("1.2.3",  1, 2, 3, null, null)]
    [TestCase("1.2",  1, 2, 0, null, null)]
    [TestCase("1",  1, 0, 0, null, null)]
    [TestCase("1.2.3-beta",  1, 2, 3, "beta", null)]
    [TestCase("1.2.3-beta3",  1, 2, 3, "beta.3", null)]
    [TestCase("1.2.3-alpha",  1, 2, 3, "alpha", null)]
    [TestCase("1.2.3-alpha4",  1, 2, 3, "alpha.4", null)]
    [TestCase("1.2.3-rc",  1, 2, 3, "rc", null)]
    [TestCase("1.2.3-rc3",  1, 2, 3, "rc.3", null)]
    [TestCase("1.2.3-RC3",  1, 2, 3, "RC.3", null)]
    [TestCase("1.2.3-rc3.1",  1, 2, 3, "rc.3", 1)]
    [TestCase("01.02.03-rc03",  1, 2, 3, "rc.3", null)]
    [TestCase("1.2.3-beta3f",  1, 2, 3, "beta3f", null)]
    [TestCase("1.2.3-notAStability1",  1, 2, 3, "notAStability.1", null)]
    public void ValidateVersionParsing(string versionString, int major, int minor, int patch, string tag, int? preReleaseTwo)
    {
        SemanticVersion version;
        Assert.IsTrue(SemanticVersionParser.TryParse(versionString, out version), "TryParse Result");
        Assert.AreEqual(major, version.Major);
        Assert.AreEqual(minor, version.Minor);
        Assert.AreEqual(patch, version.Patch);
        Assert.AreEqual(tag, version.PreReleaseTag.ToString());
        Assert.AreEqual(preReleaseTwo, version.BuildMetaData == null ? null : version.BuildMetaData.CommitsSinceTag);
    }

    [TestCase("someText")]
    [TestCase("1.2.3.4")] // TODO Maybe we should allow this to be parsed and 4th part is preReleaseTwo
    [TestCase("some-T-ext")]
    public void ValidateInvalidVersionParsing(string versionString)
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersionParser.TryParse(versionString, out version), "TryParse Result");
    }

}