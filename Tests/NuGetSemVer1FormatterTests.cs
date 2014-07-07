namespace Tests
{
    using GitVersion;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class NuGetSemVer1FormatterTests
    {

        [TestCase(0,1,0,"5.Branch.feature/my-blah-22", "0.1.0-bMyBlah22c005")]
        [TestCase(0,1,0,"5.Branch.my-blah-22", "0.1.0-bMyBlah22c005")]
        [TestCase(0,1,0,"4.Branch.feature-22", "0.1.0-bFeature22c004")]
        [TestCase(0,1,0,"4.Branch.something/pull/my-pull-22", "0.1.0-bMyPull22c004")] //Is this a realistic test for GitHub pull requests?
        [TestCase(0,1,0,"13.Branch.develop", "0.1.0-dev013")]
        [TestCase(0,1,0,"0.Branch.develop", "0.1.0-dev000")]
        [TestCase(0, 1, 0, "5.Branch.hotfix-0.1.1", "0.1.0-fix005")]
        [TestCase(0, 1, 0, "0.Branch.hotfix-0.1.1", "0.1.0-fix000")]
        [TestCase(0, 1, 0, "0.Branch.hotfix", "0.1.0-fix000")]
        [TestCase(0, 1, 0, "0.Branch.hotfix/hotfix-0.1.1", "0.1.0-fix000")]
        [TestCase(0, 1, 0, "0.Branch.hotfix/0.1.1", "0.1.0-fix000")]
        [TestCase(0,1,0,"5.Branch.release-0.1.0", "0.1.0-rc005")]
        [TestCase(0, 1, 0, "0.Branch.release-0.1.0", "0.1.0-rc000")]
        [TestCase(0, 1, 0, "5.Branch.release/release-0.1.0", "0.1.0-rc005")]
        [TestCase(0, 1, 0, "0.Branch.release/0.1.0", "0.1.0-rc000")]
        [TestCase(0, 1, 0, "0.Branch.release", "0.1.0-rc000")]
        [TestCase(0, 1, 0, "0.Branch.master", "0.1.0")]
        [TestCase(0, 1, 0, "5.Branch.support-net35", "0.1.0-sptNet35c005")]              
        public void GetVersion(int major, int minor, int patch, string buildMetadata, string expectedVersion)
        {
            var semVer = new SemanticVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                BuildMetaData = buildMetadata
            };
            NuGetSemVer1Formatter.GetVersion(semVer).ShouldBe(expectedVersion);
        }
    }
}