using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class FeatureBranchTests
{
    [Test]
    public void Feature_branch_with_no_commit()
    {
        //this scenario should redirect to the develop finder since there is no diff btw this
        // branch and the develop branch
        var version = FinderWrapper.FindVersionForCommit("c50179a2c77843245ace262b51b08af7b3b7f8fe", "featureWithNoCommits");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual(null, version.Version.Suffix);
        Assert.AreEqual(2, version.Version.PreReleaseNumber, "Should be the number of commits ahead of master");
    }
    [Test]
    public void Feature_branch_with_1_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("c33e4999a623340f028e7a55b898ea911736eb88", "featureWithOneCommit");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual("c50179a2", version.Version.Suffix, "Suffix should be the develop commit it was branched from");
        Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease is always 0 for feature branches");
    }

    [Test]
    public void Feature_branch_with_2_commits()
    {
        var version = FinderWrapper.FindVersionForCommit("9fc0dee434901e882db8e9b997d966e19880e164", "featureWithTwoCommits");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual("c50179a2", version.Version.Suffix, "Suffix should be the develop commit it was branched from");
        Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease is always 0 for feature branches");
    }
    [Test]
    public void Feature_branch_with_2_commits_but_building_an_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("22abb6aab037a3cb969c3a7642607748dc94f3cb", "featureWithTwoCommits");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual("c50179a2", version.Version.Suffix, "Suffix should be the develop commit it was branched from");
        Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease is always 0 for feature branches");
    }
}