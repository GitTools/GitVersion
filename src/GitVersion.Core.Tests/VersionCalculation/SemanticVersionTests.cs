using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests;

[TestFixture]
public class SemanticVersionTests : TestBase
{
    [TestCase("1.2.3", 1, 2, 3, "", null, null, null, null, null, null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-beta", 1, 2, 3, "beta", null, null, null, null, null, null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-beta3", 1, 2, 3, "beta", 3, null, null, null, null, "1.2.3-beta.3", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-beta.3", 1, 2, 3, "beta", 3, null, null, null, null, "1.2.3-beta.3", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-beta-3", 1, 2, 3, "beta-3", null, null, null, null, null, "1.2.3-beta-3", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-alpha", 1, 2, 3, "alpha", null, null, null, null, null, null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-rc", 1, 2, 3, "rc", null, null, null, null, null, null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-rc3", 1, 2, 3, "rc", 3, null, null, null, null, "1.2.3-rc.3", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-3", 1, 2, 3, "", 3, null, null, null, null, "1.2.3-3", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-RC3", 1, 2, 3, "RC", 3, null, null, null, null, "1.2.3-RC.3", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-rc3.1", 1, 2, 3, "rc3", 1, null, null, null, null, "1.2.3-rc3.1", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-beta3f", 1, 2, 3, "beta3f", null, null, null, null, null, null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-notAStability1", 1, 2, 3, "notAStability", 1, null, null, null, null, "1.2.3-notAStability.1", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3+4", 1, 2, 3, "", null, 4, null, null, null, null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3+4.Branch.Foo", 1, 2, 3, "", null, 4, "Foo", null, null, null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3+randomMetaData", 1, 2, 3, "", null, null, null, null, "randomMetaData", null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3-beta.1+4.Sha.12234.Othershiz", 1, 2, 3, "beta", 1, 4, null, "12234", "Othershiz", null, null, SemanticVersionFormat.Strict)]
    [TestCase("1.2.3", 1, 2, 3, "", null, null, null, null, null, null, ConfigurationConstants.DefaultTagPrefix, SemanticVersionFormat.Strict)]
    [TestCase("v1.2.3", 1, 2, 3, "", null, null, null, null, null, "1.2.3", ConfigurationConstants.DefaultTagPrefix, SemanticVersionFormat.Strict)]
    [TestCase("V1.2.3", 1, 2, 3, "", null, null, null, null, null, "1.2.3", ConfigurationConstants.DefaultTagPrefix, SemanticVersionFormat.Strict)]
    [TestCase("version-1.2.3", 1, 2, 3, "", null, null, null, null, null, "1.2.3", "version-", SemanticVersionFormat.Strict)]
    [TestCase("1.0.0-develop-20201007113711", 1, 0, 0, "develop-20201007113711", null, null, null, null, null, "1.0.0-develop-20201007113711", null, SemanticVersionFormat.Strict)]
    [TestCase("20201007113711.658165168461351.64136516984163213-develop-20201007113711.98848747823+65416321321", 20201007113711, 658165168461351, 64136516984163213, "develop-20201007113711", 98848747823, 65416321321, null, null, null, "20201007113711.658165168461351.64136516984163213-develop-20201007113711.98848747823+65416321321", null, SemanticVersionFormat.Strict)]
    [TestCase("1.2", 1, 2, 0, "", null, null, null, null, null, "1.2.0", null, SemanticVersionFormat.Loose)]
    [TestCase("1.2-alpha4", 1, 2, 0, "alpha", 4, null, null, null, null, "1.2.0-alpha.4", null, SemanticVersionFormat.Loose)]
    [TestCase("01.02.03-rc03", 1, 2, 3, "rc", 3, null, null, null, null, "1.2.3-rc.3", null, SemanticVersionFormat.Loose)]
    [TestCase("1.2.3.4", 1, 2, 3, "", null, 4, null, null, null, "1.2.3+4", null, SemanticVersionFormat.Loose)]
    [TestCase("1", 1, 0, 0, "", null, null, null, null, null, "1.0.0", null, SemanticVersionFormat.Loose)]
    [TestCase("1.1", 1, 1, 0, "", null, null, null, null, null, "1.1.0", null, SemanticVersionFormat.Loose)]
    public void ValidateVersionParsing(
        string? versionString, long major, long minor, long patch, string label, long? tagNumber, long? numberOfBuilds,
        string? branchName, string? sha, string? otherMetaData, string? fullFormattedVersionString, string? tagPrefixRegex, SemanticVersionFormat format = SemanticVersionFormat.Strict)
    {
        fullFormattedVersionString ??= versionString;

        versionString.ShouldNotBeNull();
        SemanticVersion.TryParse(versionString, tagPrefixRegex, out var version, format).ShouldBe(true, versionString);

        version.ShouldNotBeNull();
        Assert.Multiple(() =>
        {
            Assert.That(version.Major, Is.EqualTo(major));
            Assert.That(version.Minor, Is.EqualTo(minor));
            Assert.That(version.Patch, Is.EqualTo(patch));
            Assert.That(version.PreReleaseTag.Name, Is.EqualTo(label));
            Assert.That(version.PreReleaseTag.Number, Is.EqualTo(tagNumber));

            Assert.That(version.BuildMetaData.CommitsSinceTag, Is.EqualTo(numberOfBuilds));
            Assert.That(version.BuildMetaData.Branch, Is.EqualTo(branchName));
            Assert.That(version.BuildMetaData.Sha, Is.EqualTo(sha));
            Assert.That(version.BuildMetaData.OtherMetaData, Is.EqualTo(otherMetaData));
            Assert.That(version.ToString("i"), Is.EqualTo(fullFormattedVersionString));
        });
    }

    [TestCase("someText")]
    [TestCase("some-T-ext")]
    [TestCase("v.1.2.3", ConfigurationConstants.DefaultTagPrefix)]
    public void ValidateInvalidVersionParsing(string versionString, string? tagPrefixRegex = null) =>
        Assert.That(SemanticVersion.TryParse(versionString, tagPrefixRegex, out _), Is.False, "TryParse Result");

    [Test]
    public void VersionSorting()
    {
        SemanticVersion.Parse("1.0.0", null).ShouldBeGreaterThan(SemanticVersion.Parse("1.0.0-beta", null));
        SemanticVersion.Parse("1.0.0-beta.2", null).ShouldBeGreaterThan(SemanticVersion.Parse("1.0.0-beta.1", null));
        SemanticVersion.Parse("1.0.0-beta.1", null).ShouldBeLessThan(SemanticVersion.Parse("1.0.0-beta.2", null));
    }

    [Test]
    public void ToStringWithInvalidFormatTest()
    {
        var semVer = BuildSemVer(1, 2, 3, "rc", 1, 1);
        Should.Throw<FormatException>(() => semVer.ToString("invalid"));
    }

    [TestCase(1, 2, 3, null, null, null, null, null, null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "beta", 4, null, null, null, null, ExpectedResult = "1.2.3-beta.4")]
    [TestCase(1, 2, 3, "beta", 4, 5, "theBranch", "theSha", "theOtherMetaData", ExpectedResult = "1.2.3-beta.4")]
    [TestCase(1, 2, 3, "", 4, 5, "theBranch", "theSha", "theOtherMetaData", ExpectedResult = "1.2.3-4")]
    public string ToStringTests(int major, int minor, int patch, string? preReleaseName, int? preReleaseVersion, int? buildCount, string? branchName, string? sha, string? otherMetadata)
    {
        var semVer = BuildSemVer(major, minor, patch, preReleaseName, preReleaseVersion, buildCount, branchName, sha, otherMetadata);
        return semVer.ToString();
    }

    [TestCase(1, 2, 3, null, null, null, null, null, null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "beta", 4, null, null, null, null, ExpectedResult = "1.2.3-beta.4")]
    [TestCase(1, 2, 3, "beta", 4, 5, "theBranch", "theSha", "theOtherMetaData", ExpectedResult = "1.2.3-beta.4")]
    [TestCase(1, 2, 3, "", 4, 10, "theBranch", "theSha", "theOtherMetaData", ExpectedResult = "1.2.3-4")]
    public string ToStringWithSFormatTests(int major, int minor, int patch, string? preReleaseName, int? preReleaseVersion, int? buildCount, string? branchName, string? sha, string? otherMetadata)
    {
        var semVer = BuildSemVer(major, minor, patch, preReleaseName, preReleaseVersion, buildCount, branchName, sha, otherMetadata);
        return semVer.ToString("s");
    }

    [TestCase(1, 2, 3, null, null, null, null, null, null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "beta", 4, null, null, null, null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "beta", 4, 5, "theBranch", "theSha", "theOtherMetaData", ExpectedResult = "1.2.3")]
    public string ToStringWithFormatJTests(int major, int minor, int patch, string? preReleaseName, int? preReleaseVersion, int? buildCount, string? branchName, string? sha, string? otherMetadata)
    {
        var semVer = BuildSemVer(major, minor, patch, preReleaseName, preReleaseVersion, buildCount, branchName, sha, otherMetadata);
        return semVer.ToString("j");
    }

    [TestCase(1, 2, 3, null, null, null, null, null, null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "beta", 4, null, null, null, null, ExpectedResult = "1.2.3-beta.4")]
    [TestCase(1, 2, 3, "beta", 4, 5, null, null, null, ExpectedResult = "1.2.3-beta.4+5")]
    [TestCase(1, 2, 3, "", 4, 5, "theBranch", "theSha", "theOtherMetaData", ExpectedResult = "1.2.3-4+5")]
    public string ToStringWithFormatFTests(int major, int minor, int patch, string? preReleaseName, int? preReleaseVersion, int? buildCount, string? branchName, string? sha, string? otherMetadata)
    {
        var semVer = BuildSemVer(major, minor, patch, preReleaseName, preReleaseVersion, buildCount, branchName, sha, otherMetadata);
        return semVer.ToString("f");
    }

    [TestCase(1, 2, 3, null, null, null, null, null, null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "beta", 4, null, null, null, null, ExpectedResult = "1.2.3-beta.4")]
    [TestCase(1, 2, 3, "beta", 4, 5, null, null, null, ExpectedResult = "1.2.3-beta.4+5")]
    [TestCase(1, 2, 3, "beta", 4, 5, "theBranch", "theSha", "theOtherMetaData", ExpectedResult = "1.2.3-beta.4+5.Branch.theBranch.Sha.theSha.theOtherMetaData")]
    public string ToStringWithFormatITests(int major, int minor, int patch, string? preReleaseName, int? preReleaseVersion, int? buildCount, string? branchName, string? sha, string? otherMetadata)
    {
        var semVer = BuildSemVer(major, minor, patch, preReleaseName, preReleaseVersion, buildCount, branchName, sha, otherMetadata);
        return semVer.ToString("i");
    }

    private static SemanticVersion BuildSemVer(int major, int minor, int patch, string? preReleaseName, int? preReleaseVersion,
        int? buildCount, string? branchName = null, string? sha = null, string? otherMetadata = null)
    {
        SemanticVersionPreReleaseTag? preReleaseTag = null;
        if (preReleaseName != null)
        {
            preReleaseTag = new SemanticVersionPreReleaseTag(preReleaseName, preReleaseVersion, true);
        }

        SemanticVersionBuildMetaData? buildMetaDate = null;
        if (buildCount.HasValue)
        {
            buildMetaDate = new SemanticVersionBuildMetaData
            {
                CommitsSinceTag = buildCount.Value,
                Sha = sha,
                Branch = branchName,
                OtherMetaData = otherMetadata
            };
        }

        return new SemanticVersion(major, minor, patch)
        {
            PreReleaseTag = preReleaseTag ?? SemanticVersionPreReleaseTag.Empty,
            BuildMetaData = buildMetaDate ?? SemanticVersionBuildMetaData.Empty
        };
    }
}
