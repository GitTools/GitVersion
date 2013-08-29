using GitFlowVersion;
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
        var version = FinderWrapper.FindVersionForCommit("6b503e747408bbbcac7ec20a6c81cf10e53b6dcd", "hotfix-0.1.1");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(1, version.Patch);
        Assert.AreEqual(Stage.Beta, version.Stage);
        Assert.AreEqual(1, version.PreRelease);
    }

    [Test]
    public void Commit_two_in_front_of_initial_branch()
    {
        var version = FinderWrapper.FindVersionForCommit("8530d6a72140355b5004a878630cdf596ff551e1", "hotfix-0.1.1");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(1, version.Patch);
        Assert.AreEqual(Stage.Beta, version.Stage);
        Assert.AreEqual(2, version.PreRelease);
    }

}