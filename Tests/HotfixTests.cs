using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class HotfixTests
{
//* major = semver(branch name without prefix).major
//* minor= semver(branch name without prefix).minor
//* patch= semver(branch name without prefix).patch
//* prerelease: beta{number of commits on branch}
    [Test]
    public void Commit_one_in_front_of_initial_branch()
    {
        var version = FinderWrapper.FindVersionForCommit("1732b6a6c362f4a422dd2fb24ae8e78b46689715", "hotfix-0.1.1");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(1, version.Patch);
        Assert.AreEqual(Stage.Beta, version.Stage);
        Assert.AreEqual(1, version.PreRelease);
    }

    [Test]
    public void Commit_two_in_front_of_initial_branch()
    {
        var version = FinderWrapper.FindVersionForCommit("8d81f9ff761d7f73a73c585b86026f1bbf1e148a", "hotfix-0.1.1");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(1, version.Patch);
        Assert.AreEqual(Stage.Beta, version.Stage);
        Assert.AreEqual(2, version.PreRelease);
    }

}