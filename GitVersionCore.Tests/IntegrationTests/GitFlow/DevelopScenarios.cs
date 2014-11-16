using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class DevelopScenarios
{
    [Test]
    public void WhenDevelopBranchedFromMaster_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.AssertFullSemver("1.1.0-unstable.0+0");
        }
    }
    
    [Test]
    public void CanChangeDevelopTagViaConfig()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config
        {
            DevelopBranchTag = "alpha"
        }))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.AssertFullSemver("1.1.0-alpha.0+0");
        }
    }
    
    [Test]
    public void CanClearDevelopTagViaConfig()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config
        {
            DevelopBranchTag = ""
        }))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.AssertFullSemver("1.1.0+0");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromMasterDetachedHead_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            var commit = fixture.Repository.Head.Tip;
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(commit);
            fixture.AssertFullSemver("1.1.0-unstable.1+1");
        }
    }
}