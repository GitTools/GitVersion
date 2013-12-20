using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class VersionOnMasterFinderTests
{

    [Test]
    public void Should_find_previous_commit_that_was_at_least_a_minor_bump()
    {
        var finder = new VersionOnMasterFinder();
        var version = finder.Execute(new GitFlowVersionContext
        {
            Repository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        new MockMergeCommit
                        {
                            MessageEx = "Merge branch 'hotfix-0.3.0'",
                            CommitterEx = 2.Seconds().Ago().ToSignature()
                        },
                        new MockMergeCommit
                        {
                            MessageEx = "Merge branch 'hotfix-0.3.1'",
                            CommitterEx = 2.Seconds().Ago().ToSignature(),
                        },
                        new MockMergeCommit
                        {
                            MessageEx = "Merge branch 'hotfix-0.2.0'",
                            CommitterEx = 2.Seconds().Ago().ToSignature()
                        },
                    },
                }
            }
        }, 1.Seconds().Ago());
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor);
    }

}