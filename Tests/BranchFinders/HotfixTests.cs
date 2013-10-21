using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
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
                              };
        var hotfixBranch = new MockBranch("hotfix-0.1.4")
                            {
                                branchingCommit,
                            };

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
                                                     },
                                          Tags = new MockTagCollection
                                                 {
                                                     new MockTag
                                                     {
                                                         TargetEx = branchingCommit,
                                                         NameEx = "0.1.4-alpha5"
                                                     }
                                                 }
                                      },
                     };
        var version = finder.FindVersion();

        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(4, version.Version.Patch);
        Assert.AreEqual(Stability.Alpha, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
        Assert.IsNull(version.Version.PreReleasePartTwo, "PreReleasePartTwo null since there is no commits");
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
                                                     },
                                          Tags = new MockTagCollection
                                                 {
                                                     new MockTag
                                                     {
                                                         TargetEx = branchingCommit,
                                                         NameEx = "0.1.3-beta4"
                                                     }
                                                 }
                                      },
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(3, version.Version.Patch);
        Assert.AreEqual(Stability.Beta, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(4, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 4 from the tag");
        Assert.AreEqual(1, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 1 since there is a commit on the branch");
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
                                                     },
                                          Tags = new MockTagCollection
                                                 {
                                                     new MockTag
                                                     {
                                                         TargetEx = branchingCommit,
                                                         NameEx = "0.1.3-alpha5"
                                                     }
                                                 }
                                      },
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(1, version.Version.Minor);
        Assert.AreEqual(3, version.Version.Patch);
        Assert.AreEqual(Stability.Alpha, version.Version.Stability);
        Assert.AreEqual(BranchType.Hotfix, version.BranchType);
        Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be 5 from the tag");
        Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 2 since there is 2 commits on the branch");
    }


}