using FluentDate;
using FluentDateTimeOffset;
using GitVersion;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class MasterTests
{

    [Test]
    public void Should_throw_if_head_isnt_a_merge_commit_and_no_override_tag_is_found()
    {
        var commit = new MockCommit
                     {
                         MessageEx = "Not a merge commit",
                         CommitterEx = 2.Seconds().Ago().ToSignature()
                     };
        var finder = new MasterVersionFinder();

        var exception = Assert.Throws<ErrorException>(() => finder.FindVersion(new MockRepository
        {
            Branches = new MockBranchCollection
            {
                new MockBranch("master")
                {
                    commit
                },
            }
        }, commit));
        Assert.AreEqual("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.", exception.Message);
    }

    [Test]
    public void Commit_in_front_of_tag_should_return_tag_as_version()
    {
       //should throw
    }

    [Test]
    public void Hotfix_merge()
    {
        var hotfixMergeCommit = new MockMergeCommit
                         {
                             MessageEx = "Merge branch 'hotfix-0.1.5'",
                             CommitterEx = 2.Seconds().Ago().ToSignature()
                         };
        var finder = new MasterVersionFinder();
        var version = finder.FindVersion(
            new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        hotfixMergeCommit
                    },
                }
            }, hotfixMergeCommit);

        Assert.AreEqual(5, version.Patch, "Should set the patch version to the patch of the latest hotfix merge commit");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
    }

    [Test]
    public void Override_using_tag_with_a_stable_release()
    {
        var commit = new MockCommit
        {
            CommitterEx = 2.Seconds().Ago().ToSignature()
        };
        var finder = new MasterVersionFinder();
        var version = finder.FindVersion(
            new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        commit
                    },
                },
                Tags = new MockTagCollection
                {
                    new MockTag
                    {
                        NameEx = "0.2.0",
                        TargetEx = commit
                    }
                }
            }, commit);
        Assert.AreEqual(0, version.Patch, "Should set the patch version to the patch of the latest hotfix merge commit");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
    }

    [Test]
    [Ignore("should we support overriding with a beta?")]
    public void Override_using_tag_with_a_prerelease()
    {
        var commit = new MockCommit
        {
            CommitterEx = 2.Seconds().Ago().ToSignature()
        };
        var finder = new MasterVersionFinder();
        var version = finder.FindVersion(
            new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        commit
                    },
                },
                Tags = new MockTagCollection
                {
                    new MockTag
                    {
                        NameEx = "0.1.0-beta1",
                        TargetEx = commit
                    }
                }
            }, commit);
        Assert.AreEqual(0, version.Patch, "Should set the patch version to the patch of the latest hotfix merge commit");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
    }


    [Test]
    public void Release_merge()
    {
        var commit = new MockMergeCommit
        {
            CommitterEx = 2.Seconds().Ago().ToSignature(),
            MessageEx = "Merge branch 'release-0.2.0'"
        };
        var finder = new MasterVersionFinder();
        var version = finder.FindVersion(
            new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        commit
                    },
                },
            }, commit);
        Assert.AreEqual(0, version.Patch, "Should set the patch version to 0");
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
    }

}