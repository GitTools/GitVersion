using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class HotfixTests
{
    [Test]
    public void No_commits()
    {
        var branchingCommit = new MockCommit
                              {
                                  CommitterEx = 1.Seconds().Ago().ToSignature(),
                                  IdEx = new ObjectId("c50179a2c77843245ace262b51b08af7b3b7f8fe")
                              };
        var hotfixBranch = new MockBranch("hotfix-0.1.4");

        var finder = new HotfixVersionFinder
                     {
                         HotfixBranch = hotfixBranch,
                         Commit = branchingCommit,
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             branchingCommit
                                                         },
                                                         hotfixBranch
                                                     }
                                      },
                         IsOnMasterBranchFunc = x => ReferenceEquals(x, branchingCommit)
                     };
        var version = finder.FindVersion();

        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(4, version.Version.Patch);
        Assert.AreEqual(Stability.Beta, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(0, version.Version.PreReleaseNumber, "Prerelease should be set to 0 since there is no commits");
    }

    [Test]
    public void First_commit()
    {
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "branching commit",
                              };
        var firstCommit = new MockCommit
                         {
                             MessageEx = "first commit on hotfix",
                         };
        var hotfixBranch = new MockBranch("hotfix-0.1.3")
                           {
                               firstCommit,
                               branchingCommit,
                           };
        var finder = new HotfixVersionFinder
                     {
                         HotfixBranch = hotfixBranch,
                         Commit = firstCommit,
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                            branchingCommit
                                                         },
                                                         hotfixBranch
                                                     }
                                      },
                         IsOnMasterBranchFunc = x => ReferenceEquals(x, branchingCommit)
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(3, version.Version.Patch);
        Assert.AreEqual(Stability.Beta, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(1, version.Version.PreReleaseNumber, "Prerelease should be set to 1 since there is a commit on the branch");
    }

    [Test]
    public void Second_commit()
    {
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "branchingCommit"
                              };
        var secondCommit = new MockCommit
                         {
                             MessageEx = "secondCommit"
                         };
        var hotfixBranch = new MockBranch("hotfix-0.1.3")
                           {
                               secondCommit,
                               new MockCommit
                               {
                                  MessageEx = "firstCommit"
                               },
                               branchingCommit,
                           };
        var finder = new HotfixVersionFinder
                     {
                         HotfixBranch = hotfixBranch,
                         Commit = secondCommit,
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             branchingCommit
                                                         },
                                                         hotfixBranch
                                                     }
                                      },
                         IsOnMasterBranchFunc = x => ReferenceEquals(x, branchingCommit)
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(3, version.Version.Patch);
        Assert.AreEqual(Stability.Beta, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(2, version.Version.PreReleaseNumber, "Prerelease should be set to 2 since there is 2 commits on the branch");
    }


}