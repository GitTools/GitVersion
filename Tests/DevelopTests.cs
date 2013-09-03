using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class DevelopTests
{
    [Test,Ignore("Not relevant for now")]
    public void No_commits()
    {
       
    }

    [Test]
    public void Commit_on_develop_and_previous_commit_on_master_is_a_hotfix()
    {
        var version = FinderWrapper.FindVersionForCommit("42c23e25d3bf31a3d7a54a0fdd3678209af468a2", "develop");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(2, version.Minor,"Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Unstable, version.Stage);
        Assert.AreEqual(1, version.PreRelease, "Prerelease should to the number of commits ahead of master(by date)");
    }
}