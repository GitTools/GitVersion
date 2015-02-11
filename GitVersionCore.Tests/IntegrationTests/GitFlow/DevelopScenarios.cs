using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class DevelopScenarios
{
    [Test]
    public void WhenDevelopBranchedFromTaggedCommitOnMasterVersionDoesNotChange()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.AssertFullSemver("1.0.0+0");
        }
    }

    [Test]
    public void CanChangeDevelopTagViaConfig()
    {
        var config = new Config();
        config.Branches["develop"].Tag = "alpha";
        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1+1");
        }
    }

    [Test]
    public void CanClearDevelopTagViaConfig()
    {
        var config = new Config();
        config.Branches["develop"].Tag = "";
        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0+1");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromMaster_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-unstable.1+1");
        }
    }

    [Test]
    public void MergingReleaseBranchBackIntoDevelopWithoutMergingToMaster_DoesNotBumpDevelopVersion()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("release-2.0.0").Checkout();
            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.Checkout("develop");
            fixture.AssertFullSemver("1.1.0-unstable.1+1");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());
            fixture.AssertFullSemver("1.1.0-unstable.1+1");
        }
    }

    [Test]
    public void CanHandleContinuousDelivery()
    {
        var config = new Config();
        config.Branches["develop"].VersioningMode = VersioningMode.ContinuousDelivery;
        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeATaggedCommit("1.1.0-alpha7");
            fixture.AssertFullSemver("1.1.0-alpha.7+0");
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