using GitVersion;
using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class SemanticVersionTests : TestBase
    {

        [TestCase("1.2.3", 1, 2, 3, null, null, null, null, null, null, null, null)]
        [TestCase("1.2", 1, 2, 0, null, null, null, null, null, null, "1.2.0", null)]
        [TestCase("1.2.3-beta", 1, 2, 3, "beta", null, null, null, null, null, null, null)]
        [TestCase("1.2.3-beta3", 1, 2, 3, "beta", 3, null, null, null, null, "1.2.3-beta.3", null)]
        [TestCase("1.2.3-beta.3", 1, 2, 3, "beta", 3, null, null, null, null, "1.2.3-beta.3", null)]
        [TestCase("1.2.3-beta-3", 1, 2, 3, "beta-3", null, null, null, null, null, "1.2.3-beta-3", null)]
        [TestCase("1.2.3-alpha", 1, 2, 3, "alpha", null, null, null, null, null, null, null)]
        [TestCase("1.2-alpha4", 1, 2, 0, "alpha", 4, null, null, null, null, "1.2.0-alpha.4", null)]
        [TestCase("1.2.3-rc", 1, 2, 3, "rc", null, null, null, null, null, null, null)]
        [TestCase("1.2.3-rc3", 1, 2, 3, "rc", 3, null, null, null, null, "1.2.3-rc.3", null)]
        [TestCase("1.2.3-RC3", 1, 2, 3, "RC", 3, null, null, null, null, "1.2.3-RC.3", null)]
        [TestCase("1.2.3-rc3.1", 1, 2, 3, "rc3", 1, null, null, null, null, "1.2.3-rc3.1", null)]
        [TestCase("01.02.03-rc03", 1, 2, 3, "rc", 3, null, null, null, null, "1.2.3-rc.3", null)]
        [TestCase("1.2.3-beta3f", 1, 2, 3, "beta3f", null, null, null, null, null, null, null)]
        [TestCase("1.2.3-notAStability1", 1, 2, 3, "notAStability", 1, null, null, null, null, "1.2.3-notAStability.1", null)]
        [TestCase("1.2.3.4", 1, 2, 3, null, null, 4, null, null, null, "1.2.3+4", null)]
        [TestCase("1.2.3+4", 1, 2, 3, null, null, 4, null, null, null, null, null)]
        [TestCase("1.2.3+4.Branch.Foo", 1, 2, 3, null, null, 4, "Foo", null, null, null, null)]
        [TestCase("1.2.3+randomMetaData", 1, 2, 3, null, null, null, null, null, "randomMetaData", null, null)]
        [TestCase("1.2.3-beta.1+4.Sha.12234.Othershiz", 1, 2, 3, "beta", 1, 4, null, "12234", "Othershiz", null, null)]
        [TestCase("1.2.3", 1, 2, 3, null, null, null, null, null, null, null, Config.DefaultTagPrefix)]
        [TestCase("v1.2.3", 1, 2, 3, null, null, null, null, null, null, "1.2.3", Config.DefaultTagPrefix)]
        [TestCase("V1.2.3", 1, 2, 3, null, null, null, null, null, null, "1.2.3", Config.DefaultTagPrefix)]
        [TestCase("version-1.2.3", 1, 2, 3, null, null, null, null, null, null, "1.2.3", "version-")]
        public void ValidateVersionParsing(
            string versionString, int major, int minor, int patch, string tag, int? tagNumber, int? numberOfBuilds,
            string branchName, string sha, string otherMetaData, string fullFormattedVersionString, string tagPrefixRegex)
        {
            fullFormattedVersionString ??= versionString;

            SemanticVersion.TryParse(versionString, tagPrefixRegex, out var version).ShouldBe(true, versionString);
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
            Assert.IsFalse(SemanticVersion.TryParse(versionString, null, out _), "TryParse Result");
        }

        [Test]
        public void LegacySemVerTest()
        {
            new SemanticVersionPreReleaseTag("TKT-2134_JiraDescription", null).ToString("l").ShouldBe("TKT-2134");
            new SemanticVersionPreReleaseTag("AReallyReallyReallyLongBranchName", null).ToString("l").ShouldBe("AReallyReallyReallyL");
            new SemanticVersionPreReleaseTag("TKT-2134_JiraDescription", 1).ToString("lp").ShouldBe("TKT-2134-0001");
            new SemanticVersionPreReleaseTag("TKT-2134", 1).ToString("lp").ShouldBe("TKT-2134-0001");
            new SemanticVersionPreReleaseTag("AReallyReallyReallyLongBranchName", 1).ToString("lp").ShouldBe("AReallyReallyRea0001");
        }

        [Test]
        public void VersionSorting()
        {
            SemanticVersion.Parse("1.0.0", null).ShouldBeGreaterThan(SemanticVersion.Parse("1.0.0-beta", null));
            SemanticVersion.Parse("1.0.0-beta.2", null).ShouldBeGreaterThan(SemanticVersion.Parse("1.0.0-beta.1", null));
            SemanticVersion.Parse("1.0.0-beta.1", null).ShouldBeLessThan(SemanticVersion.Parse("1.0.0-beta.2", null));
        }

        [Test]
        public void ToStringJTests()
        {
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3", null).ToString("j"));
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3-beta.4", null).ToString("j"));
            var fullSemVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = new SemanticVersionPreReleaseTag("beta", 4),
                BuildMetaData = new SemanticVersionBuildMetaData
                {
                    Sha = "theSha",
                    Branch = "TheBranch",
                    CommitsSinceTag = 5,
                    OtherMetaData = "TheOtherMetaData"
                }
            };
            Assert.AreEqual("1.2.3", fullSemVer.ToString("j"));
        }
        [Test]
        public void ToStringSTests()
        {
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3", null).ToString("s"));
            Assert.AreEqual("1.2.3-beta.4", SemanticVersion.Parse("1.2.3-beta.4", null).ToString("s"));
            var fullSemVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = new SemanticVersionPreReleaseTag("beta", 4),
                BuildMetaData = new SemanticVersionBuildMetaData
                {
                    Sha = "theSha",
                    Branch = "TheBranch",
                    CommitsSinceTag = 5,
                    OtherMetaData = "TheOtherMetaData"
                }
            };
            Assert.AreEqual("1.2.3-beta.4", fullSemVer.ToString("s"));
        }
        [Test]
        public void ToStringLTests()
        {
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3", null).ToString("l"));
            Assert.AreEqual("1.2.3-beta4", SemanticVersion.Parse("1.2.3-beta.4", null).ToString("l"));
            var fullSemVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = new SemanticVersionPreReleaseTag("beta", 4),
                BuildMetaData = new SemanticVersionBuildMetaData
                {
                    Sha = "theSha",
                    Branch = "TheBranch",
                    CommitsSinceTag = 5,
                    OtherMetaData = "TheOtherMetaData"
                }
            };
            Assert.AreEqual("1.2.3-beta4", fullSemVer.ToString("l"));
        }
        [Test]
        public void ToStringLpTests()
        {
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3", null).ToString("lp"));
            Assert.AreEqual("1.2.3-beta0004", SemanticVersion.Parse("1.2.3-beta.4", null).ToString("lp"));
            var fullSemVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = new SemanticVersionPreReleaseTag("beta", 4),
                BuildMetaData = new SemanticVersionBuildMetaData
                {
                    Sha = "theSha",
                    Branch = "TheBranch",
                    CommitsSinceTag = 5,
                    OtherMetaData = "TheOtherMetaData"
                }
            };
            Assert.AreEqual("1.2.3-beta0004", fullSemVer.ToString("lp"));
        }
        [Test]
        public void ToStringTests()
        {
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3", null).ToString());
            Assert.AreEqual("1.2.3-beta.4", SemanticVersion.Parse("1.2.3-beta.4", null).ToString());
            Assert.AreEqual("1.2.3-beta.4", SemanticVersion.Parse("1.2.3-beta.4+5", null).ToString());
            var fullSemVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = new SemanticVersionPreReleaseTag("beta", 4),
                BuildMetaData = new SemanticVersionBuildMetaData
                {
                    Sha = "theSha",
                    Branch = "TheBranch",
                    CommitsSinceTag = 5,
                    OtherMetaData = "TheOtherMetaData"
                }
            };
            Assert.AreEqual("1.2.3-beta.4", fullSemVer.ToString());
        }
        [Test]
        public void ToStringFTests()
        {
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3", null).ToString("f"));
            Assert.AreEqual("1.2.3-beta.4", SemanticVersion.Parse("1.2.3-beta.4", null).ToString("f"));
            Assert.AreEqual("1.2.3-beta.4+5", SemanticVersion.Parse("1.2.3-beta.4+5", null).ToString("f"));
            var fullSemVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = new SemanticVersionPreReleaseTag("beta", 4),
                BuildMetaData = new SemanticVersionBuildMetaData
                {
                    Sha = "theSha",
                    Branch = "TheBranch",
                    CommitsSinceTag = 5,
                    OtherMetaData = "TheOtherMetaData"
                }
            };
            Assert.AreEqual("1.2.3-beta.4+5", fullSemVer.ToString("f"));
        }
        [Test]
        public void ToStringITests()
        {
            Assert.AreEqual("1.2.3-beta.4", SemanticVersion.Parse("1.2.3-beta.4", null).ToString("i"));
            Assert.AreEqual("1.2.3", SemanticVersion.Parse("1.2.3", null).ToString("i"));
            Assert.AreEqual("1.2.3-beta.4+5", SemanticVersion.Parse("1.2.3-beta.4+5", null).ToString("i"));
            var fullSemVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = new SemanticVersionPreReleaseTag("beta", 4),
                BuildMetaData = new SemanticVersionBuildMetaData
                {
                    Sha = "theSha",
                    Branch = "TheBranch",
                    CommitsSinceTag = 5,
                    OtherMetaData = "TheOtherMetaData"
                }
            };
            Assert.AreEqual("1.2.3-beta.4+5.Branch.TheBranch.Sha.theSha.TheOtherMetaData", fullSemVer.ToString("i"));
        }
    }
}
