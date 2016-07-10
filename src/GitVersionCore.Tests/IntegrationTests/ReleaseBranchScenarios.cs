using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class ReleaseBranchScenarios
{
    [Test]
    public void NoMergeBacksToDevelopInCaseThereAreNoChangesInReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeCommits(3);
            var releaseBranch = fixture.Repository.CreateBranch("release/1.0.0");
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.Repository.ApplyTag("1.0.0");
            fixture.Repository.Checkout("develop");

            fixture.Repository.Branches.Remove(releaseBranch);

            fixture.AssertFullSemver("1.1.0-alpha.0");
        }
    }

    [Test]
    public void NoMergeBacksToDevelopInCaseThereAreChangesInReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeCommits(3);
            var releaseBranch = fixture.Repository.CreateBranch("release/1.0.0");
            fixture.Repository.Checkout(releaseBranch);
            fixture.Repository.MakeACommit();

            // Merge to master
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.Repository.ApplyTag("1.0.0");

            // Merge to develop
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.AssertFullSemver("1.1.0-alpha.2");

            fixture.Repository.MakeACommit();
            fixture.Repository.Branches.Remove(releaseBranch);

            fixture.AssertFullSemver("1.1.0-alpha.2");
        }
    }

    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }
    }

    [Test]
    public void CanTakeVersionFromReleasesBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("releases/2.0.0");
            fixture.Repository.Checkout("releases/2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }
    }

    [Test]
    public void ReleaseBranchWithNextVersionSetInConfig()
    {
        var config = new Config
        {
            NextVersion = "2.0.0"
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeCommits(5);
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("release-2.0.0"));

            fixture.AssertFullSemver(config, "2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver(config, "2.0.0-beta.1+2");
        }
    }

    [Test]
    public void CanTakeVersionFromReleaseBranchWithTagOverridden()
    {
        var config = new Config
        {
            Branches =
            {
                { "releases?[/-]", new BranchConfig { Tag = "rc" } }
            }
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver(config, "2.0.0-rc.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver(config, "2.0.0-rc.1+2");
        }
    }

    [Test]
    public void CanHandleReleaseBranchWithStability()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0-Final");
            fixture.Repository.Checkout("release-2.0.0-Final");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }
    }

    [Test]
    public void WhenReleaseBranch_OffDevelop_IsMergedIntoMasterAndDevelop_VersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0+2");
        }
    }

    [Test]
    public void WhenReleaseBranch_OffMaster_IsMergedIntoMaster_VersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
        }
    }

    [Test]
    public void MasterVersioningContinuousCorrectlyAfterMergingReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
            fixture.Repository.ApplyTag("2.0.0");
            fixture.Repository.MakeCommits(1);
            fixture.AssertFullSemver("2.0.1+1");
        }
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoDevelopHighestVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Repository.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.1.0-alpha.6");
        }
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoMasterHighestVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Repository.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+5");
        }
    }

    [Test]
    public void WhenMergingReleaseBackToDevShouldNotResetBetaVersion()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");

            fixture.Repository.MakeCommits(1);
            fixture.AssertFullSemver("1.1.0-alpha.1");

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.1+1");

            //tag it to bump to beta 2
            fixture.Repository.ApplyTag("2.0.0-beta1");

            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.2+2");

            //merge down to develop
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            //but keep working on the release
            fixture.Repository.Checkout("release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.2+2");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.2+3");
        }
    }

    [Test]
    public void HotfixOffReleaseBranchShouldNotResetCount()
    {
        var config = new Config
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");

            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver(config, "2.0.0-beta.1");

            //tag it to bump to beta 2
            fixture.Repository.MakeCommits(4);

            fixture.AssertFullSemver(config, "2.0.0-beta.5");

            //merge down to develop
            fixture.Repository.CreateBranch("hotfix-2.0.0");
            fixture.Repository.MakeCommits(2);

            //but keep working on the release
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MergeNoFF("hotfix-2.0.0", Generate.SignatureNow());
            fixture.Repository.Branches.Remove(fixture.Repository.Branches["hotfix-2.0.0"]);
            fixture.AssertFullSemver(config, "2.0.0-beta.7");
        }
    }
    [Test]
    public void MergeOnReleaseBranchShouldNotResetCount()
    {
        var config = new Config
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
            VersioningMode = VersioningMode.ContinuousDeployment,
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");
            fixture.Repository.MakeACommit();

            fixture.Repository.CreateBranch("release/2.0.0");

            fixture.Repository.CreateBranch("release/2.0.0-xxx");
            fixture.Repository.Checkout("release/2.0.0-xxx");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver(config, "2.0.0-beta.1");

            fixture.Repository.Checkout("release/2.0.0");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver(config, "2.0.0-beta.1");

            fixture.Repository.MergeNoFF("release/2.0.0-xxx");
            fixture.AssertFullSemver(config, "2.0.0-beta.2");
        }
    }
}