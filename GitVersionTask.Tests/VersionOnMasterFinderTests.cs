using System;
using FluentDate;
using FluentDateTimeOffset;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class VersionOnMasterFinderTests
{
    [Test]
    public void Should_find_previous_commit_that_was_at_least_a_minor_bump()
    {
        var finder = new VersionOnMasterFinder();

        var dateTime = new DateTimeOffset(2000, 10, 10, 0, 0, 0, new TimeSpan(0));
        var signature = 2.Seconds().Before(dateTime).ToSignature();

        const string sha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";

        var repository = new MockRepository
        {
            Branches = new MockBranchCollection
                {
                    new MockBranch("master")
                    {
                        new MockMergeCommit(new ObjectId(sha))
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
        };
        var version = finder.Execute(new GitVersionContext(repository, null, new Config()), 1.Seconds().Ago());
        ObjectApprover.VerifyWithJson(version);
    }
}