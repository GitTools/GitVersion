using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class VersionOnMasterFinderTests
{
    [Test, Ignore("Not relevant for now")]
    public void No_commits()
    {

    }

    [Test]
    public void Should_find_previous_commit_that_was_at_least_a_minor_bump()
    {
        var finder = new VersionOnMasterFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.3.1'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature(),
                                                             },
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.3.0'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             },
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.2.0'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             },
                                                         },
                                                     }
                                      },
                         OlderThan = 1.Seconds().Ago()
                     };
        var version = finder.Execute();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor);
        Assert.AreEqual(0, version.Version.Patch);
    }
    [Test]
    public void Should_ignore_earlier_that_is_larger()
    {
        var finder = new VersionOnMasterFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.3.1'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature(),
                                                             },
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.7.0'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             },
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.3.0'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             },
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.5.0'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             },
                                                             new MockCommit
                                                             {
                                                                 MessageEx = "Merge branch 'hotfix-0.2.0'",
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             },
                                                         },
                                                     }
                                      },
                         OlderThan = 1.Seconds().Ago()
                     };
        var version = finder.Execute();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor);
        Assert.AreEqual(0, version.Version.Patch);
    }

}