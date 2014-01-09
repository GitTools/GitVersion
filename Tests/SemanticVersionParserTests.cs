using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class SemanticVersionParserTests
{

    [TestCase("1.2.3", true, 1, 2, 3, null, Stability.Final, null, null)]
    [TestCase("1.2", true, 1, 2, 0, null, Stability.Final, null, null)]
    [TestCase("1", true, 1, 0, 0, null, Stability.Final, null, null)]
    [TestCase("1.2.3-beta", true, 1, 2, 3, "beta", Stability.Beta, null, null)]
    [TestCase("1.2.3-beta3", true, 1, 2, 3, "beta3", Stability.Beta, 3, null)]
    [TestCase("1.2.3-alpha", true, 1, 2, 3, "alpha", Stability.Alpha, null, null)]
    [TestCase("1.2.3-alpha4", true, 1, 2, 3, "alpha4", Stability.Alpha, 4, null)]
    [TestCase("1.2.3-rc", true, 1, 2, 3, "rc", Stability.ReleaseCandidate, null, null)]
    [TestCase("1.2.3-rc3", true, 1, 2, 3, "rc3", Stability.ReleaseCandidate, 3, null)]
    [TestCase("1.2.3-RC3", true, 1, 2, 3, "RC3", Stability.ReleaseCandidate, 3, null)]
    [TestCase("1.2.3-rc3.1", true, 1, 2, 3, "rc3", Stability.ReleaseCandidate, 3, 1)]
    [TestCase("01.02.03-rc03", true, 1, 2, 3, "rc03", Stability.ReleaseCandidate, 3, null)]
    [TestCase("1.2.3-beta3f", true, 1, 2, 3, "beta3f", null, null, null)]
    [TestCase("1.2.3-notAStability1", true, 1, 2, 3, "notAStability1", null, 1, null)]
    [TestCase("someText", false)]
    [TestCase("1.2.3.4", false)] // TODO Maybe we should allow this to be parsed and 4th part is preReleaseTwo
    [TestCase("some-T-ext", false)]
    public void ValidateVersionParsing(string versionString, bool canParse, int major, int minor, int patch, string tag, 
        Stability? stability, int? releaseNumber, int? preReleaseTwo)
    {
        SemanticVersion version;
        Assert.AreEqual(canParse, SemanticVersionParser.TryParse(versionString, out version), "TryParse Result");
        if (canParse)
        {
            Assert.AreEqual(major, version.Major);
            Assert.AreEqual(minor, version.Minor);
            Assert.AreEqual(patch, version.Patch);
            Assert.AreEqual(stability, version.Tag.InferStability());
            Assert.AreEqual(releaseNumber, version.Tag.ReleaseNumber());
            Assert.AreEqual(preReleaseTwo, version.PreReleasePartTwo);
        }
    }

}