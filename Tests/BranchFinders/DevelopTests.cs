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
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(2, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Develop, version.BranchType);
        Assert.AreEqual(1, version.Version.PreReleaseNumber, "Prerelease should to the number of commits ahead of master(by date)");
    }

    [Test]
    public void Commit_on_develop_and_previous_commit_on_master_tag()
    {
        var version = FinderWrapper.FindVersionForCommit("6b503e747408bbbcac7ec20a6c81cf10e53b6dcd", "develop");
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(2, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Develop, version.BranchType);
        Assert.AreEqual(1, version.Version.PreReleaseNumber, "Prerelease should to the number of commits ahead of master(by date)");
    }
}