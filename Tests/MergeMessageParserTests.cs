using System.Collections.Generic;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class MergeMessageParserTests
{

    [Test]
    public void MergeHotFix()
    {
        AssertMergeMessage("0.1.5", "Merge branch 'hotfix-0.1.5'\n");
    }

    [Test]
    public void MergeHotFixWithLargeNumber()
    {
        AssertMergeMessage("10.10.50", "Merge branch 'hotfix-10.10.50'\n");
    }

    [Test]
    public void MergeRelease()
    {
        AssertMergeMessage("0.2.0", "Merge branch 'release-0.2.0'\n");
    }

    [Test]
    public void MergeReleaseNotStartingWithNumber()
    {
        AssertMergeMessage(null, "Merge branch 's'\n");
    }

    [Test]
    public void MergeReleaseWithLargeNumber()
    {
        AssertMergeMessage("10.10.50", "Merge branch 'release-10.10.50'\n");
    }

    [Test]
    public void MergeBadNamedHotfixBranch()
    {
        //TODO: possible make it a config option to support this
        AssertMergeMessage("4.0.3", "Merge branch '4.0.3'\n");
    }

    [Test]
    public void TooManyTrailingCharacters()
    {
        AssertMergeMessage(null, "Merge branch 'develop' of github.com:Particular/NServiceBus into develop\n");
    }

    [Test]
    public void NotAMergeCommit()
    {
        var c = new MockCommit
        {
            MessageEx = "Merge branch 'hotfix-0.1.5'\n",
        };

        string versionPart;
        Assert.IsFalse(MergeMessageParser.TryParse(c, out versionPart));
    }

    void AssertMergeMessage(string expectedVersion, string message)
    {
        var c = new MockCommit
                {
                    MessageEx = message,
                    ParentsEx = new List<Commit> {null, null}
                };

        string versionPart;
        var parsed = MergeMessageParser.TryParse(c, out versionPart);

        if (expectedVersion == null)
        {
            Assert.IsFalse(parsed);
        }
        else
        {
            Assert.IsTrue(parsed);
            Assert.AreEqual(expectedVersion, versionPart);
        }
    }
}
