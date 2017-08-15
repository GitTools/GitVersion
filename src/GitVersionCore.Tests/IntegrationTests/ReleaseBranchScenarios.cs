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
            fixture.Repository.MakeACommit();

            fixture.Repository.Branches.Remove(releaseBranch);

            fixture.AssertFullSemver("1.1.0-unstable.1");
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
            fixture.AssertFullSemver("1.1.0-unstable.1");

            fixture.Repository.MakeACommit();
            fixture.Repository.Branches.Remove(releaseBranch);

            fixture.AssertFullSemver("1.1.0-unstable.2");
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

            fixture.AssertFullSemver("2.1.0-unstable.5");
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
            fixture.AssertFullSemver("1.1.0-unstable.1");

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

    /// <summary>
    /// Create a feature branch from a release branch, and merge back, then delete it
    /// </summary>
    [Test]
    public void FeatureOnRelease_FeatureBranchDeleted()
    {
        var config = new Config
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new EmptyRepositoryFixture())
        {
            var release450 = "release/4.5.0";
            var featureBranch = "feature/some-bug-fix";

            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");

            // begin the release branch
            fixture.Repository.CreateBranch(release450);
            Commands.Checkout(fixture.Repository, release450);
            fixture.AssertFullSemver(config, "4.5.0-beta.0");

            fixture.Repository.CreateBranch(featureBranch);
            Commands.Checkout(fixture.Repository, featureBranch);
            fixture.Repository.MakeACommit("blabla"); // commit 1
            Commands.Checkout(fixture.Repository, release450);
            fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2
            fixture.Repository.Branches.Remove(featureBranch);

            fixture.AssertFullSemver(config, "4.5.0-beta.2");
        }
    }

    /// <summary>
    /// Create a feature branch from a release branch, and merge back, but don't delete it
    /// </summary>
    [Test]
    public void FeatureOnRelease_FeatureBranchNotDeleted()
    {
        var config = new Config
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new EmptyRepositoryFixture())
        {
            var release450 = "release/4.5.0";
            var featureBranch = "feature/some-bug-fix";

            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");

            // begin the release branch
            fixture.Repository.CreateBranch(release450);
            Commands.Checkout(fixture.Repository, release450);
            fixture.AssertFullSemver(config, "4.5.0-beta.0");

            fixture.Repository.CreateBranch(featureBranch);
            Commands.Checkout(fixture.Repository, featureBranch);
            fixture.Repository.MakeACommit("blabla"); // commit 1
            Commands.Checkout(fixture.Repository, release450);
            fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2

            fixture.AssertFullSemver(config, "4.5.0-beta.2");
        }
    }

}