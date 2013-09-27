using System;
using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class PullBranchTests
{

    [Test, Ignore("Not valid since Github wont allow empty pulls")]
    public void Pull_request_with_no_commit()
    {

    }

    [Test]
    [Explicit]
    public void Pull_branch_with_1_commit()
    {
        //TODO
        //var version = FinderWrapper.FindVersionForCommit("fa7924aabc3a0c462d2e65dd62bd35a66b88bdb4", "origin/pull/2");
        //Assert.AreEqual(0, version.Version.Major);
        //Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        //Assert.AreEqual(0, version.Version.Patch);
        //Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        //Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        //Assert.AreEqual("2", version.Version.Suffix); //in TC the branch name will be the pull request no eg 1154
        //Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }

    [Test]
    public void Pull_branch_with_1_commit_TeamCity()
    {
        FakeTeamCityPullrequest(2);
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "Merge branch 'release-0.2.0'",
                                  CommitterEx = 2.Seconds().Ago().ToSignature(),
                                  IdEx = new ObjectId("c50179a2c77843245ace262b51b08af7b3b7f8fe")
                              };
        var commitOneOnFeature = new MockCommit
                                 {
                                     CommitterEx = 1.Seconds().Ago().ToSignature(),
                                 };
        var pullBranch = new MockBranch("pull_no_2")
                         {
                             branchingCommit,
                             commitOneOnFeature
                         };

        var finder = new PullVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         pullBranch,
                                                         new MockBranch("master")
                                                         {
                                                             branchingCommit,
                                                         }
                                                     }
                                      },
                         Commit = commitOneOnFeature,
                         PullBranch = pullBranch,
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        Assert.AreEqual("2", version.Version.Suffix); //in TC the branch name will be the pull request no eg 1154
        Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }

    [Test]
    [Explicit]
    public void Pull_branch_with_2_commits()
    {
        //TODO
        //var version = FinderWrapper.FindVersionForCommit("fa7924aabc3a0c462d2e65dd62bd35a66b88bdb4", "pull_no_2");
        //Assert.AreEqual(0, version.Version.Major);
        //Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        //Assert.AreEqual(0, version.Version.Patch);
        //Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        //Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        //Assert.AreEqual("2", version.Version.Suffix); //in TC the branch name will be the pull request no eg 1154
        //Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }

    [Test]
    public void Pull_branch_with_2_commits_TeamCity()
    {
        FakeTeamCityPullrequest(2);

        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "Merge branch 'release-0.2.0'",
                                  CommitterEx = 2.Seconds().Ago().ToSignature(),
                                  IdEx = new ObjectId("c50179a2c77843245ace262b51b08af7b3b7f8fe")
                              };
        var commitTwoOnFeature = new MockCommit
                                 {
                                     CommitterEx = 1.Seconds().Ago().ToSignature(),
                                 };
        var pullBranch = new MockBranch("pull_no_2")
                         {
                             branchingCommit,
                             new MockCommit(),
                             commitTwoOnFeature
                         };

        var finder = new PullVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         pullBranch,
                                                         new MockBranch("master")
                                                         {
                                                             branchingCommit,
                                                         }
                                                     }
                                      },
                         Commit = commitTwoOnFeature,
                         PullBranch = pullBranch,
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.PullRequest, version.BranchType);
        Assert.AreEqual("2", version.Version.Suffix); //in TC the branch name will be the pull request no eg 1154
        Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease is always 0 for pull requests");
    }

    [TearDown]
    public void ClearTeamCityPullrequest()
    {
        Environment.SetEnvironmentVariable("teamcity.build.vcs.branch.NServiceBusCore_GitHubNServiceBus", "", EnvironmentVariableTarget.Process);
    }


    void FakeTeamCityPullrequest(int pullNumber)
    {
        Environment.SetEnvironmentVariable("teamcity.build.vcs.branch.NServiceBusCore_GitHubNServiceBus",
            string.Format("refs/pull/{0}/merge", pullNumber), EnvironmentVariableTarget.Process);
    }
}