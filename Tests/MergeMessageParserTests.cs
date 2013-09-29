using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class MergeMessageParserTests
{

    [Test]
    public void MergeHotFix()
    {
        string versionPart;
        MergeMessageParser.TryParse("Merge branch 'hotfix-0.1.5'\n", out versionPart);
        Assert.AreEqual("0.1.5", versionPart);
    }

    [Test]
    public void MergeHotFixWithLargeNumber()
    {
        string versionPart;
        MergeMessageParser.TryParse("Merge branch 'hotfix-10.10.50'\n", out versionPart);
        Assert.AreEqual("10.10.50", versionPart);
    }

    [Test]
    public void MergeRelease()
    {
        string versionPart;
        MergeMessageParser.TryParse("Merge branch 'release-0.2.0'\n", out versionPart);
        Assert.AreEqual("0.2.0", versionPart);
    }

    [Test]
    public void MergeReleaseNotStartingWithNumber()
    {
        string versionPart;
        Assert.IsFalse(MergeMessageParser.TryParse("Merge branch 's'\n", out versionPart));
    }

    [Test]
    public void MergeReleaseWithLargeNumber()
    {
        string versionPart;
        MergeMessageParser.TryParse("Merge branch 'release-10.10.50'\n", out versionPart);
        Assert.AreEqual("10.10.50", versionPart);
    }

    [Test]
    public void MergeBadNamedHotfixBranch()
    {
        //TODO: possible make it a config option to support this
        string versionPart;
        MergeMessageParser.TryParse("Merge branch '4.0.3'\n", out versionPart);
        Assert.AreEqual("4.0.3", versionPart);
    }

    [Test]
    public void TooManyTrailingCharacters()
    {
        string versionPart;
        Assert.IsFalse(MergeMessageParser.TryParse("Merge branch 'develop' of github.com:Particular/NServiceBus into develop\n", out versionPart));
    }

    

    
}