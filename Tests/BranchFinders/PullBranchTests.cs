using System;
using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class PullBranchTests
{

    [Test, Ignore("Not valid since Github wont allow empty pulls")]
    public void Pull_request_with_no_commit()
    {

    }



    [Test]
    public void Pull_branch_with_1_commit()
    {
        FakeTeamcityPullrequest(2);

        var version = FinderWrapper.FindVersionForCommit("fa7924aabc3a0c462d2e65dd62bd35a66b88bdb4", "pull_no_2");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Stability);
        Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        Assert.AreEqual("2", version.Suffix); //in TC the branch name will be the pull request no eg 1154
        Assert.AreEqual(0, version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }



    [Test]
    public void Pull_branch_with_2_commits()
    {
        FakeTeamcityPullrequest(2);
        
        var version = FinderWrapper.FindVersionForCommit("fa7924aabc3a0c462d2e65dd62bd35a66b88bdb4", "pull_no_2");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Stability);
        Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        Assert.AreEqual("2", version.Suffix); //in TC the branch name will be the pull request no eg 1154
        Assert.AreEqual(0, version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }

    [TearDown]
    public  void ClearTeamCityPullrequest()
    {
        Environment.SetEnvironmentVariable("teamcity.build.vcs.branch.NServiceBusCore_GitHubNServiceBu", "", EnvironmentVariableTarget.Process);
    }


    void FakeTeamcityPullrequest(int pullNumber)
    {
        Environment.SetEnvironmentVariable("teamcity.build.vcs.branch.NServiceBusCore_GitHubNServiceBu",
            string.Format("refs/pull/{0}/merge", pullNumber), EnvironmentVariableTarget.Process);
    }
}