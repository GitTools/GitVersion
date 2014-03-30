using GitVersion;
using NUnit.Framework;

[TestFixture]
public class SemanticVersionTests
{

    [TestCase("1.2.3", 1, 2, 3, null, null, null, null, null, null, null)]
    [TestCase("1.2", 1, 2, 0, null, null, null, null, null, null, "1.2.0")]
    [TestCase("1", 1, 0, 0, null, null, null, null, null, null, "1.0.0")]
    [TestCase("1.2.3-beta", 1, 2, 3, "beta", null, null, null, null, null, null)]
    [TestCase("1.2.3-beta3", 1, 2, 3, "beta", 3, null, null, null, null, null, "1.2.3-beta.3")]
    [TestCase("1.2.3-alpha", 1, 2, 3, "alpha", null, null, null, null, null, null)]
    [TestCase("1.2-alpha4", 1, 2, 0, "alpha", 4, null, null, null, null, "1.2.0-alpha.4")]
    [TestCase("1.2.3-rc", 1, 2, 3, "rc", null, null, null, null, null, null)]
    [TestCase("1.2.3-rc3", 1, 2, 3, "rc", 3, null, null, null, null, "1.2.3-rc.3")]
    [TestCase("1.2.3-RC3", 1, 2, 3, "RC", 3, null, null, null, null, "1.2.3-RC.3")]
    [TestCase("1.2.3-rc3.1", 1, 2, 3, "rc3", 1, null, null, null, null, "1.2.3-rc3.1")]
    [TestCase("01.02.03-rc03", 1, 2, 3, "rc", 3, null, null, null, null, "1.2.3-rc.3")]
    [TestCase("1.2.3-beta3f", 1, 2, 3, "beta3f", null, null, null, null, null, null)]
    [TestCase("1.2.3-notAStability1", 1, 2, 3, "notAStability", 1, null, null, null, null, "1.2.3-notAStability.1")]
    [TestCase("1.2.3.4", 1, 2, 3, null, null, 4, null, null, null, "1.2.3+4")]
    [TestCase("1.2.3+4", 1, 2, 3, null, null, 4, null, null, null, null)]
    [TestCase("1.2.3+4.Branch.Foo", 1, 2, 3, null, null, 4, "Foo", null, null, null)]
    [TestCase("1.2.3+randomMetaData", 1, 2, 3, null, null, null, null, null, "randomMetaData", null)]
    [TestCase("1.2.3-beta.1+4.Sha.12234.Othershiz", 1, 2, 3, "beta", 1, 4, null, "12234", "Othershiz", null)]
    public void ValidateVersionParsing(string versionString, int major, int minor, int patch, string tag, int? tagNumber, int? numberOfBuilds,
        string branchName, string sha, string otherMetaData, string fullFormattedVersionString)
    {
        fullFormattedVersionString = fullFormattedVersionString ?? versionString;

        SemanticVersion version;
        Assert.IsTrue(SemanticVersion.TryParse(versionString, out version), "TryParse Result");
        Assert.AreEqual(major, version.Major);
        Assert.AreEqual(minor, version.Minor);
        Assert.AreEqual(patch, version.Patch);
        Assert.AreEqual(tag, version.PreReleaseTag.Name);
        Assert.AreEqual(tagNumber, version.PreReleaseTag.Number);
        Assert.AreEqual(numberOfBuilds, version.BuildMetaData.CommitsSinceTag);
        Assert.AreEqual(branchName, version.BuildMetaData.Branch);
        Assert.AreEqual(sha, version.BuildMetaData.Sha);
        Assert.AreEqual(otherMetaData, version.BuildMetaData.OtherMetaData);
        Assert.AreEqual(fullFormattedVersionString, version.ToString("i"));
    }

    [TestCase("someText")]
    [TestCase("some-T-ext")]
    public void ValidateInvalidVersionParsing(string versionString)
    {
        SemanticVersion version;
        Assert.IsFalse(SemanticVersion.TryParse(versionString, out version), "TryParse Result");
    }


}