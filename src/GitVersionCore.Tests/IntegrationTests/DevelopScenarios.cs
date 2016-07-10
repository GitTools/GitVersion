using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class DevelopScenarios
{
    [Test]
    public void WhenDevelopHasMultipleCommits_SpecifyExistingCommitId()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));

            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            var thirdCommit = fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.0-alpha.3", commitId: thirdCommit.Sha);
        }
    }

    [Test]
    public void WhenDevelopHasMultipleCommits_SpecifyNonExistingCommitId()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));

            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.0-alpha.5", commitId: "nonexistingcommitid");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromTaggedCommitOnMasterVersionDoesNotChange()
    {
        using (var fixture = new EmptyRepositoryFixture())
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
                {
                    "dev(elop)?(ment)?$", new BranchConfig
                    {
                        Tag = "alpha"
                    }
                }
            }
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver(config, "1.1.0-alpha.1");
        }
    }

    [Test]
    public void WhenDeveloperBranchExistsDontTreatAsDevelop()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("developer"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.0.1-developer.1+1"); // this tag should be the branch name by default, not unstable
        }
    }

    [Test]
    public void WhenDevelopBranchedFromMaster_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1");
        }
    }

    [Test]
    public void MergingReleaseBranchBackIntoDevelopWithMergingToMaster_DoesBumpDevelopVersion()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("release-2.0.0"));
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.0-alpha.2");
        }
    }

    [Test]
    public void CanHandleContinuousDelivery()
    {
        var config = new Config
        {
            Branches =
            {
                {"dev(elop)?(ment)?$", new BranchConfig
                {
                    VersioningMode = VersioningMode.ContinuousDelivery
                }
                }
            }
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeATaggedCommit("1.1.0-alpha7");
            fixture.AssertFullSemver(config, "1.1.0-alpha.7");
        }
    }

    [Test]
    public void WhenDevelopBranchedFromMasterDetachedHead_MinorIsIncreased()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            var commit = fixture.Repository.Head.Tip;
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(commit);
            fixture.AssertFullSemver("1.1.0-alpha.1");
        }
    }

    [Test]
    public void InheritVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.MakeATaggedCommit("1.0.0");
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/2.0.0");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.Checkout("develop");
            fixture.AssertFullSemver("2.1.0-alpha.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.1.0-alpha.1");
            fixture.MergeNoFF("release/2.0.0");
            fixture.AssertFullSemver("2.1.0-alpha.4");
            fixture.BranchTo("feature/MyFeature");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.1.0-MyFeature.1+3");
        }
    }
}