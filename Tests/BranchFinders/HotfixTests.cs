using System;
using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class HotfixTests
{
    [Test]
    public void No_commits()
    {
        var version = FinderWrapper.FindVersionForCommit("42c23e25d3bf31a3d7a54a0fdd3678209af468a2", "hotfix-0.1.4");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(4, version.Version.Patch);
        Assert.AreEqual(Stability.Beta, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease should be set to 0 since there is no commits");
    }

    [Test]
    public void First_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("e6d69d0511400087cf09f2f6a4e97588b3b669d2", "hotfix-0.1.3");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(3, version.Version.Patch);
        Assert.AreEqual(Stability.Beta, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(1, version.Version.PreReleaseNumber, "Prerelease should be set to 1 since there is a commit on the branch");
    }

    [Test]
    public void Second_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("3b09cebcbb4a6eadf534b2b5b7ee4b778a4e49c1", "hotfix-0.1.3");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(3, version.Version.Patch);
        Assert.AreEqual(Stability.Beta, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(2, version.Version.PreReleaseNumber, "Prerelease should be set to 2 since there is 2 commits on the branch");
    }

    [Test, Ignore("Not really going to happen in real life")]
    public void After_merge_to_master()
    {
        Assert.Throws<Exception>(() => FinderWrapper.FindVersionForCommit("8530d6a72140355b5004a878630cdf596ff551e1", "hotfix-0.1.1"));
    }

}