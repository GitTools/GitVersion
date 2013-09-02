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
        var version = FinderWrapper.FindVersionForCommit("c50179a2c77843245ace262b51b08af7b3b7f8fe", "a_pull_request");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Unstable, version.Stage);
        Assert.AreEqual(null, version.Suffix);
        Assert.AreEqual(2, version.PreRelease, "Should be the number of commits ahead of master");
    }
    [Test]
    public void Feature_branch_with_1_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("f1c44cb2a8fe960b6638c487cb6d70092fdaa432", "a_pull_request");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Feature, version.Stage);
        Assert.AreEqual("c50179a2", version.Suffix);
        Assert.AreEqual(0, version.PreRelease, "Prerelease is always 0 for feature branches");
    }

    [Test]
    public void Feature_branch_with_2_commits()
    {
        var version = FinderWrapper.FindVersionForCommit("72cbf7b589e3edf3b0e781624471446c2a49037e", "a_pull_request");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Feature, version.Stage);
        Assert.AreEqual("c50179a2", version.Suffix);
        Assert.AreEqual(0, version.PreRelease, "Prerelease is always 0 for feature branches");
    }
}