using System.Collections.Generic;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class MergeMessageParserTests
{

    [TestCase("Merge branch 'hotfix-0.1.5'", false, null)]
    [TestCase("Merge branch 'develop' of github.com:Particular/NServiceBus into develop", true, null)]
    [TestCase("Merge branch '4.0.3'", true, "4.0.3")] //TODO: possible make it a config option to support this
    [TestCase("Merge branch 'release-10.10.50'", true, "10.10.50")]
    [TestCase("Merge branch 's'", true, null)] // Must start with a number
    [TestCase("Merge branch 'release-0.2.0'", true, "0.2.0")]
    [TestCase("Merge branch 'hotfix-4.6.6' into support-4.6", true, "4.6.6")]
    [TestCase("Merge branch 'hotfix-10.10.50'", true, "10.10.50")]
    [TestCase("Merge tag '10.10.50'", true, "10.10.50")]
    [TestCase("Merge branch 'hotfix-0.1.5'", true, "0.1.5")]
    [TestCase("Merge branch 'hotfix-0.1.5'\n\nRelates to: TicketId", true, "0.1.5")]
    [TestCase("Merge branch 'alpha-0.1.5'", true, "0.1.5")]
    [TestCase("Merge pull request #165 from Particular/release-1.0.0", true, "1.0.0")]
    [TestCase("Merge pull request #95 from Particular/issue-94", false, null)]
    [TestCase("Merge pull request #165 in Particular/release-1.0.0", true, "1.0.0")]
    [TestCase("Merge pull request #95 in Particular/issue-94", true, null)]
    [TestCase("Merge pull request #95 in Particular/issue-94", false, null)]
    [TestCase("Merge pull request #64 from arledesma/feature-VS2013_3rd_party_test_framework_support", true, null)]
    [TestCase("Finish Release-0.12.0", true, "0.12.0")] //Support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Release' branch
    [TestCase("Finish 0.14.1", true, "0.14.1")] //Support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Hotfix' branch
    public void AssertMergeMessage(string message, bool isMergeCommit, string expectedVersion)
    {
        var parents = GetParents(isMergeCommit);
        AssertMereMessage(message, expectedVersion, parents);
        AssertMereMessage(message+ " ", expectedVersion, parents);
        AssertMereMessage(message+"\r ", expectedVersion, parents);
        AssertMereMessage(message+"\r", expectedVersion, parents);
        AssertMereMessage(message+"\r\n", expectedVersion, parents);
        AssertMereMessage(message+"\r\n ", expectedVersion, parents);
        AssertMereMessage(message+"\n", expectedVersion, parents);
        AssertMereMessage(message+"\n ", expectedVersion, parents);
    }

    static void AssertMereMessage(string message, string expectedVersion, List<Commit> parents)
    {
        var commit = new MockCommit
        {
            MessageEx = message,
            ParentsEx = parents
        };

        SemanticVersion versionPart;
        var parsed = MergeMessageParser.TryParse(commit, new Config(), out versionPart);

        if (expectedVersion == null)
        {
            parsed.ShouldBe(false);
        }
        else
        {
            parsed.ShouldBe(true);
            var versionAsString = string.Format("{0}.{1}.{2}", versionPart.Major, versionPart.Minor, versionPart.Patch);
            versionAsString.ShouldBe(expectedVersion);
        }
    }

    static List<Commit> GetParents(bool isMergeCommit)
    {
        if (isMergeCommit)
        {
            return new List<Commit>
            {
                null,
                null
            };
        }
        return new List<Commit>
        {
            null
        };
    }
}
