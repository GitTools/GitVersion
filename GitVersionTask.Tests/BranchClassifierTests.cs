using GitVersion;
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
    public void IsSupport()
    {
        Assert.IsTrue(new MockBranch("support-1").IsSupport());
        Assert.IsTrue(new MockBranch("support/1").IsSupport());
        Assert.IsTrue(new MockBranch("Support-1").IsSupport());
        Assert.IsTrue(new MockBranch("Support/1").IsSupport());
        Assert.IsFalse(new MockBranch("release").IsRelease());
    }
    [Test]
    public void IsDevelop()
    {
        Assert.IsTrue(new MockBranch("develop").IsDevelop(new Config()));
        Assert.IsTrue(new MockBranch("Develop").IsDevelop(new Config()));
        Assert.IsFalse(new MockBranch("hotfix1").IsDevelop(new Config()));
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
