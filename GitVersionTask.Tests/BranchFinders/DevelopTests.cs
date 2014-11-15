using FluentDate;
using FluentDateTimeOffset;
using GitVersion;
using GitVersion.Configuration;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class DevelopTests
{
    [Test]
    [Ignore] //TODO Delete?
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
        var repository = new MockRepository
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
        };
        var version = finder.FindVersion(new GitVersionContext(repository, mockBranch, new Config()));
        Assert.AreEqual(2, version.Minor, "Minor should be master.Minor+1");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
    }

    [Test]
    [Ignore] //TODO Delete?
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
        var repository = new MockRepository
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
        };
        var context = new GitVersionContext(repository, develop, new Config());

        var version = finder.FindVersion(context);
        Assert.AreEqual(2, version.Minor, "Minor should be master.Minor+1");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
    }
}