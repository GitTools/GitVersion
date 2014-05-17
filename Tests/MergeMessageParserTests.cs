using System.Collections.Generic;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class MergeMessageParserTests
{

    [TestCase("Merge branch 'hotfix-0.1.5'\n", false, null)]
    [TestCase("Merge branch 'develop' of github.com:Particular/NServiceBus into develop\n", true, null)]
    [TestCase("Merge branch '4.0.3'\n", true, "4.0.3")] //TODO: possible make it a config option to support this
    [TestCase("Merge branch 'release-10.10.50'\n", true, "10.10.50")]
    [TestCase("Merge branch 's'\n", true, null)] // Must start with a number
    [TestCase("Merge branch 'release-0.2.0'\n", true, "0.2.0")]
    [TestCase("Merge branch 'hotfix-10.10.50'\n", true, "10.10.50")]
    [TestCase("Merge branch 'hotfix-0.1.5'\n", true, "0.1.5")]
    [TestCase("Merge branch 'hotfix-0.1.5'\n\nRelates to: TicketId", true, "0.1.5")]
    [TestCase("Merge branch 'alpha-0.1.5'", true, "0.1.5")]
    [TestCase("Merge pull request #165 from Particular/release-1.0.0", true, "1.0.0")]
    public void AssertMergeMessage(string message, bool isMergeCommit, string expectedVersion)
    {
        var c = new MockCommit
                {
                    MessageEx = message,
                    ParentsEx = isMergeCommit ? new List<Commit> {null, null} : new List<Commit>{ null }
                };

        string versionPart;
        var parsed = MergeMessageParser.TryParse(c, out versionPart);

        if (expectedVersion == null)
        {
            parsed.ShouldBe(false);
        }
        else
        {
            parsed.ShouldBe(true);
            versionPart.ShouldBe(expectedVersion);
        }
    }
}
