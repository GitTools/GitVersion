using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class DevelopScenarios
{
    [Test]
    public void WhenDevelopHasMultipleCommits_SpecifyExistingCommitId()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));

            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            var thirdCommit = fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.0-unstable.3", commitId: thirdCommit.Sha);
        }
    }

    [Test]
    public void WhenDevelopHasMultipleCommits_SpecifyNonExistingCommitId()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));

            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.0-unstable.5", commitId: "nonexistingcommitid");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromTaggedCommitOnMasterVersionDoesNotChange()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.AssertFullSemver("1.0.0");
        }
    }

    [Test]
    public void CanChangeDevelopTagViaConfig()
    {
        var config = new Config
        {
            Branches =
            {
                {"develop", new BranchConfig
                {
                    Tag = "alpha"
                }
                }
            }
        };
        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromMaster_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-unstable.1");
        }
    }

    [Test]
    public void MergingReleaseBranchBackIntoDevelopWithMergingToMaster_DoesBumpDevelopVersion()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("release-2.0.0"));
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());
            fixture.AssertFullSemver("2.1.0-unstable.0");
        }
    }

    [Test]
    public void CanHandleContinuousDelivery()
    {
        var config = new Config
        {
            Branches =
            {
                {"develop", new BranchConfig
                {
                    VersioningMode = VersioningMode.ContinuousDelivery
                }
                }
            }
        };
        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeATaggedCommit("1.1.0-alpha7");
            fixture.AssertFullSemver("1.1.0-alpha.7");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromMasterDetachedHead_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            var commit = fixture.Repository.Head.Tip;
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(commit);
            fixture.AssertFullSemver("1.1.0-unstable.1");
        }
    }
}