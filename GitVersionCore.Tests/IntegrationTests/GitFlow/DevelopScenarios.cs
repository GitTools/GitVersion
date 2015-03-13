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
            var branch = fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout(branch);
            fixture.AssertFullSemver("1.1.0-unstable.0+0");
        }
    }

    [Test]
    public void MergingReleaseBranchBackIntoDevelopWithoutMergingToMaster_DoesNotBumpDevelopVersion()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            var developBranch = fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout(developBranch);
            var releaseBranch = fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout(releaseBranch);
            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.Checkout("develop");
            fixture.AssertFullSemver("1.1.0-unstable.0+0");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());
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
            var branch = fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout(branch);
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
            var branch = fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout(branch);
            fixture.AssertFullSemver("1.1.0+0");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromMasterDetachedHead_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            var branch = fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout(branch);
            fixture.Repository.MakeACommit();
            var commit = fixture.Repository.Head.Tip;
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(commit);
            fixture.AssertFullSemver("1.1.0-unstable.1+1");
        }
    }
}