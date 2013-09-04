using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class MergeMessageParserTests
{

    [Test]
    public void MergeHotFix()
    {
        var versionFromMergeCommit = MergeMessageParser.GetVersionFromMergeCommit("Merge branch 'hotfix-0.1.5'\n");
        Assert.AreEqual("0.1.5", versionFromMergeCommit);
    }

    [Test]
    public void MergeHotFixWithLargeNumber()
    {
        var versionFromMergeCommit = MergeMessageParser.GetVersionFromMergeCommit("Merge branch 'hotfix-10.10.50'\n");
        Assert.AreEqual("10.10.50", versionFromMergeCommit);
    }
    [Test]
    public void MergeRelease()
    {
        var versionFromMergeCommit = MergeMessageParser.GetVersionFromMergeCommit("Merge branch 'release-0.2.0'\n");
        Assert.AreEqual("0.2.0", versionFromMergeCommit);
    }
    [Test]
    public void MergeReleaseWithLargeNumber()
    {
        var versionFromMergeCommit = MergeMessageParser.GetVersionFromMergeCommit("Merge branch 'release-10.10.50'\n");
        Assert.AreEqual("10.10.50", versionFromMergeCommit);
    }
    [Test]
    public void MergeBadNamedHotfixBranch()
    {
        //TODO: possible make it a config option to support this
        var versionFromMergeCommit = MergeMessageParser.GetVersionFromMergeCommit("Merge branch '4.0.3'\n");
        Assert.AreEqual("4.0.3", versionFromMergeCommit);
    }

    
}