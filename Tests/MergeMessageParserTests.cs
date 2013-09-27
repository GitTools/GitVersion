using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class MergeMessageParserTests
{

    [Test]
    public void MergeHotFix()
    {
        string versionFromMergeCommit;
        MergeMessageParser.TryParse("Merge branch 'hotfix-0.1.5'\n", out versionFromMergeCommit);
        Assert.AreEqual("0.1.5", versionFromMergeCommit);
    }

    [Test]
    public void MergeHotFixWithLargeNumber()
    {
        string versionFromMergeCommit;
        MergeMessageParser.TryParse("Merge branch 'hotfix-10.10.50'\n", out versionFromMergeCommit);
        Assert.AreEqual("10.10.50", versionFromMergeCommit);
    }
    [Test]
    public void MergeRelease()
    {
        string versionFromMergeCommit;
        MergeMessageParser.TryParse("Merge branch 'release-0.2.0'\n", out versionFromMergeCommit);
        Assert.AreEqual("0.2.0", versionFromMergeCommit);
    }
    [Test]
    public void MergeReleaseWithLargeNumber()
    {
        string versionFromMergeCommit;
        MergeMessageParser.TryParse("Merge branch 'release-10.10.50'\n", out versionFromMergeCommit);
        Assert.AreEqual("10.10.50", versionFromMergeCommit);
    }
    [Test]
    public void MergeBadNamedHotfixBranch()
    {
        //TODO: possible make it a config option to support this
        string versionFromMergeCommit;
        MergeMessageParser.TryParse("Merge branch '4.0.3'\n", out versionFromMergeCommit);
        Assert.AreEqual("4.0.3", versionFromMergeCommit);
    }

    
}