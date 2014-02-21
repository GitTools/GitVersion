using System;
using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class VersionOnMasterFinderTests
{

    [Test]
    public void Should_find_previous_commit_that_was_at_least_a_minor_bump()
    {
        var finder = new VersionOnMasterFinder();

        var dateTime = new DateTimeOffset(2000, 10, 10,0,0,0,new TimeSpan(0));
        var signature = 2.Seconds().Before(dateTime).ToSignature();
        var version = finder.Execute(new GitVersionContext
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
                            CommitterEx = signature
                        },
                        new MockMergeCommit
                        {
                            MessageEx = "Merge branch 'hotfix-0.3.1'",
                            CommitterEx = signature,
                        },
                        new MockMergeCommit
                        {
                            MessageEx = "Merge branch 'hotfix-0.2.0'",
                            CommitterEx = signature
                        },
                    },
                }
            }
        }, 1.Seconds().Ago());
        ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
    }

}