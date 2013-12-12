using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class BranchClassifierTests
{

    [Test]
    public void IsHotfix()
    {
        Assert.IsTrue(new MockBranch("hotfix-1").IsHotfix());
        Assert.IsTrue(new MockBranch("hotfix/1").IsHotfix());
        Assert.IsTrue(new MockBranch("Hotfix-1").IsHotfix());
        Assert.IsTrue(new MockBranch("Hotfix/1").IsHotfix());
        Assert.IsFalse(new MockBranch("hotfix1").IsHotfix());
    }
    [Test]
    public void IsRelease()
    {
        Assert.IsTrue(new MockBranch("release-1").IsRelease());
        Assert.IsTrue(new MockBranch("release/1").IsRelease());
        Assert.IsTrue(new MockBranch("Release-1").IsRelease());
        Assert.IsTrue(new MockBranch("Release/1").IsRelease());
        Assert.IsFalse(new MockBranch("release").IsRelease());
    }
    [Test]
    public void IsDevelop()
    {
        Assert.IsTrue(new MockBranch("develop").IsDevelop());
        Assert.IsTrue(new MockBranch("Develop").IsDevelop());
        Assert.IsFalse(new MockBranch("hotfix1").IsDevelop());
    }
    [Test]
    public void IsMaster()
    {
        Assert.IsTrue(new MockBranch("master").IsMaster());
        Assert.IsTrue(new MockBranch("Master").IsMaster());
        Assert.IsFalse(new MockBranch("hotfix1").IsMaster());
    }
    [Test]
    public void IsPullRequest()
    {
        Assert.IsTrue(new MockBranch("fix for issue xxx","/pull/4").IsPullRequest());
        Assert.IsFalse(new MockBranch("hotfix1","").IsPullRequest());
    }
}
