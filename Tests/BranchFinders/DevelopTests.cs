using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using NUnit.Framework;

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
        var finder = new DevelopVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "hotfix-0.1.1",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             }
                                                         },
                                                         new MockBranch("develop")
                                                         {
                                                             commitOnDevelop
                                                         }
                                                     }
                                      },
                         Commit = commitOnDevelop
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(2, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Develop, version.BranchType);
        Assert.AreEqual(1, version.Version.PreReleasePartOne, "Prerelease should to the number of commits ahead of master(by date)");
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
        var finder = new DevelopVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             commitOnMaster
                                                         },
                                                         new MockBranch("develop")
                                                         {
                                                             commitOnDevelop
                                                         }
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
                         Commit = commitOnDevelop
                     };

        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(2, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Develop, version.BranchType);
        Assert.AreEqual(1, version.Version.PreReleasePartOne, "Prerelease should to the number of commits ahead of master(by date)");
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
        var commitOneOnMaster = new MockCommit
                             {
                                 CommitterEx = 4.Seconds().Ago().ToSignature(),
                             };
        var commitTwoOnMaster = new MockCommit
                             {
                                 CommitterEx = 3.Seconds().Ago().ToSignature(),
                             };
        var commitThreeOnMaster = new MockCommit
                             {
                                 CommitterEx = 2.Seconds().Ago().ToSignature(),
                             };
        var finder = new DevelopVersionFinder
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
                                                         new MockBranch("develop")
                                                         {
                                                             commitTwoOnDevelop,
                                                             commitOneOnDevelop
                                                         }
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
                                                 }
                                      },
                         Commit = commitTwoOnDevelop
                     };

        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(4, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Develop, version.BranchType);
        Assert.AreEqual(2, version.Version.PreReleasePartOne, "Prerelease should to the number of commits ahead of master(by date)");
    }
}