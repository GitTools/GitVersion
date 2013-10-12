using System;
using FluentDate;
using FluentDateTimeOffset;
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
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "Merge branch 'release-0.2.0'",
                                  CommitterEx = 2.Seconds().Ago().ToSignature(),
                              };
        var commitOneOnFeature = new MockCommit
                                 {
                                     CommitterEx = 1.Seconds().Ago().ToSignature(),
                                 };
        var pullBranch = new MockBranch("2","/pull/2")
                         {
                             branchingCommit,
                             commitOneOnFeature,
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
        Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for pull requests");
    }

    [Test]
    public void Pull_branch_with_1_commit_TeamCity()
    {
        FakeTeamCityPullrequest(2);
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "Merge branch 'release-0.2.0'",
                                  CommitterEx = 2.Seconds().Ago().ToSignature(),
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
        Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for pull requests");
    }

    [Test]
    public void Pull_branch_with_2_commits()
    {
        var branchingCommit = new MockCommit
        {
            MessageEx = "Merge branch 'release-0.2.0'",
            CommitterEx = 2.Seconds().Ago().ToSignature(),
        };
        var commitTwoOnFeature = new MockCommit
        {
            CommitterEx = 1.Seconds().Ago().ToSignature(),
        };
        var pullBranch = new MockBranch("2", "/pull/2")
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
        Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for pull requests");
    }

    [Test]
    public void Pull_branch_with_2_commits_TeamCity()
    {
        FakeTeamCityPullrequest(2);

        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "Merge branch 'release-0.2.0'",
                                  CommitterEx = 2.Seconds().Ago().ToSignature(),
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
        Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for pull requests");
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