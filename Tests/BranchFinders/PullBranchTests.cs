using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class PullBranchTests
{
  
    [Test,Ignore("Not valid since Github wont allow empty pulls")]
    public void Pull_request_with_no_commit()
    {

    }
    [Test]
    public void Pull_branch_with_1_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("0fbb31549b83b342b32306d03364affaf675f44c", "origin/pull/2");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Stability);
        Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        Assert.AreEqual("2", version.Suffix);
        Assert.AreEqual(0, version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }

    [Test]
    public void Pull_branch_with_2_commits()
    {
        var version = FinderWrapper.FindVersionForCommit("fa7924aabc3a0c462d2e65dd62bd35a66b88bdb4", "origin/pull/2");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Stability);
        Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        Assert.AreEqual("2", version.Suffix);
        Assert.AreEqual(0, version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }
}