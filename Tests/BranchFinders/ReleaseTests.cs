using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class ReleaseTests
{
    [Test]
    public void No_commits()
    {
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "branching commit",
                              CommitterEx = 1.Seconds().Ago().ToSignature()
                              };
        var releaseBranch = new MockBranch("release-0.3.0")
                            {
                                branchingCommit,
                            };
        var finder = new ReleaseVersionFinder
                     {
                         ReleaseBranch = releaseBranch,
                         Commit = branchingCommit,
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit
                                                         },
                                                         releaseBranch
                                                     },
                                          Tags = new MockTagCollection
                                                 {
                                                     new MockTag
                                                     {
                                                         TargetEx = branchingCommit,
                                                         NameEx = "0.3.0-alpha5"
                                                     }
                                                 }
                                      },
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor);
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Alpha, version.Version.Stability);
        Assert.AreEqual(BranchType.Release, version.BranchType);
        Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
        Assert.IsNull(version.Version.PreReleasePartTwo, "PreReleasePartTwo null since there is no commits");
    }

    [Test]
    public void First_commit()
    {
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "branching commit",
                                  CommitterEx = 2.Seconds().Ago().ToSignature()
                              };
        var firstCommit = new MockCommit
                          {
                              MessageEx = "first commit on release",
                              CommitterEx = 1.Seconds().Ago().ToSignature()
                          };
        var releaseBranch = new MockBranch("release-0.5.0")
                            {
                                firstCommit,
                                branchingCommit,
                            };
        var finder = new ReleaseVersionFinder
                     {
                         ReleaseBranch = releaseBranch,
                         Commit = firstCommit,
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit
                                                         },
                                                         releaseBranch
                                                     },
                                          Tags = new MockTagCollection
                                                 {
                                                     new MockTag
                                                     {
                                                         TargetEx = branchingCommit,
                                                         NameEx = "0.5.0-alpha5"
                                                     }
                                                 }
                                      },
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(5, version.Version.Minor);
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Alpha, version.Version.Stability);
        Assert.AreEqual(BranchType.Release, version.BranchType);
        Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
        Assert.AreEqual(1, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 1 since there is 1 commit");
    }

    [Test]
    public void Second_commit()
    {
        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "branching commit",
                              CommitterEx = 3.Seconds().Ago().ToSignature()
                              };
        var secondCommit = new MockCommit
                           {
                               MessageEx = "second commit on release",
                              CommitterEx = 1.Seconds().Ago().ToSignature()
                           };
        var firstCommit = new MockCommit
                         {
                             MessageEx = "first commit on release",
                              CommitterEx = 2.Seconds().Ago().ToSignature()
                         };
        var releaseBranch = new MockBranch("release-0.4.0")
                            {
                                secondCommit,
                                firstCommit,
                                branchingCommit,
                            };
        var finder = new ReleaseVersionFinder
                     {
                         ReleaseBranch = releaseBranch,
                         Commit = secondCommit,
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit
                                                         },
                                                         releaseBranch
                                                     },
                                          Tags = new MockTagCollection
                                                 {
                                                     new MockTag
                                                     {
                                                         TargetEx = branchingCommit,
                                                         NameEx = "0.4.0-alpha5"
                                                     }
                                                 }
                                      },
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(4, version.Version.Minor);
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Alpha, version.Version.Stability);
        Assert.AreEqual(BranchType.Release, version.BranchType);
        Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
        Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 3 since there is 2 commits on the branch");
    }


    [Test]
    public void Override_stage_using_tag()
    {

        var branchingCommit = new MockCommit
                              {
                                  MessageEx = "branching commit",
                              CommitterEx = 2.Seconds().Ago().ToSignature()
                              };
        var firstCommit = new MockCommit
                          {
                              MessageEx = "first commit on release",
                              CommitterEx = 1.Seconds().Ago().ToSignature()
                          };
        var releaseBranch = new MockBranch("release-0.4.0")
                            {
                                firstCommit,
                                branchingCommit,
                            };
        var finder = new ReleaseVersionFinder
                     {
                         ReleaseBranch = releaseBranch,
                         Commit = firstCommit,
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit
                                                         },
                                                         releaseBranch
                                                     },
                                          Tags = new MockTagCollection
                                                 {
                                                     new MockTag
                                                     {
                                                         TargetEx = branchingCommit,
                                                         NameEx = "0.4.0-RC4"
                                                     }
                                                 }
                                      },
                     };
        var version = finder.FindVersion();
        //tag: 0.4.0-RC1 => 
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(4, version.Version.Minor);
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.ReleaseCandidate, version.Version.Stability);
        Assert.AreEqual(BranchType.Release, version.BranchType);
        Assert.AreEqual(4, version.Version.PreReleasePartOne);
    }

        //TODO:
    //[Test]
    //[ExpectedException]
    //public void Override_stage_using_tag_should_throw_on_version_mismatch()
    //{
    //    var version = FinderWrapper.FindVersionForCommit("34dbc768fcbdd57d6089fe28f9d37472b9e97e35", "release-0.5.0");
    //}

    [Test, Ignore("Not really going to happen in real life se we skip this for now")]
    public void After_merge_to_master()
    {
        //TODO
        //Assert.Throws<Exception>(() => FinderWrapper.FindVersionForCommit("TODO", "release-0.5.0"));
    }

}