using FluentDate;
using FluentDateTimeOffset;
using GitVersion;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class DevelopTests
{
    [Test, Ignore("Not relevant for now")]
    public void No_commits()
    {

    }

    [Test]
    public void Commit_on_develop_and_previous_commit_on_master_is_a_hotfix()
    {
        var commitOnDevelop = new MockCommit
                              {
                                  CommitterEx = 1.Seconds().Ago().ToSignature()
                              };
        var finder = new DevelopVersionFinder();
        var mockBranch = new MockBranch("develop")
        {
            commitOnDevelop
        };
        var version = finder.FindVersion(new GitVersionContext
        {
            Repository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        new MockMergeCommit
                        {
                            MessageEx = "hotfix-0.1.1",
                            CommitterEx = 2.Seconds().Ago().ToSignature()
                        }
                    },
                    mockBranch
                },
            },
            CurrentBranch = mockBranch
        });
        Assert.AreEqual(2, version.Version.Minor, "Minor should be master.Minor+1");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);

    }

    [Test]
    public void Commit_on_develop_and_previous_commit_on_master_has_a_tag()
    {
        var commitOnDevelop = new MockCommit
                              {
                                  CommitterEx = 1.Seconds().Ago().ToSignature()
                              };
        var commitOnMaster = new MockCommit
                             {
                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                             };
        var finder = new DevelopVersionFinder();
        var develop = new MockBranch("develop")
        {
            commitOnDevelop
        };
        var context = new GitVersionContext
        {
            Repository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        commitOnMaster
                    },
                    develop
                },
                Tags = new MockTagCollection
                {
                    new MockTag
                    {
                        TargetEx = commitOnMaster,
                        NameEx = "0.1.0"
                    }
                }
            },
            CurrentBranch = develop
        };

        var version = finder.FindVersion(context);
        Assert.AreEqual(2, version.Version.Minor, "Minor should be master.Minor+1");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
    }
    [Test]
    public void Multiple_minor_versions_on_master()
    {
        var commitOneOnDevelop = new MockCommit
                              {
                                  CommitterEx = 1.Seconds().Ago().ToSignature()
                              };
        var commitTwoOnDevelop = new MockCommit
                              {
                                  CommitterEx = 1.Seconds().Ago().ToSignature()
                              };
        var commitOneOnMaster = new MockMergeCommit
                             {
                                 CommitterEx = 4.Seconds().Ago().ToSignature(),
                             };
        var commitTwoOnMaster = new MockMergeCommit
                             {
                                 CommitterEx = 3.Seconds().Ago().ToSignature(),
                             };
        var commitThreeOnMaster = new MockMergeCommit
                             {
                                 CommitterEx = 2.Seconds().Ago().ToSignature(),
                             };
        var finder = new DevelopVersionFinder();
        var develop = new MockBranch("develop")
        {
            commitTwoOnDevelop,
            commitOneOnDevelop
        };
        var context = new GitVersionContext
        {
            Repository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        commitThreeOnMaster,
                        commitTwoOnMaster,
                        commitOneOnMaster,
                    },
                    develop
                },
                Tags = new MockTagCollection
                {
                    new MockTag
                    {
                        TargetEx = commitOneOnMaster,
                        NameEx = "0.2.0"
                    },
                    new MockTag
                    {
                        TargetEx = commitTwoOnMaster,
                        NameEx = "0.3.0"
                    },
                    new MockTag
                    {
                        TargetEx = commitThreeOnMaster,
                        NameEx = "0.3.3"
                    }
                },
            },
            CurrentBranch = develop
        };

        var version = finder.FindVersion(context);
        Assert.AreEqual(4, version.Version.Minor, "Minor should be master.Minor+1");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
    }
}