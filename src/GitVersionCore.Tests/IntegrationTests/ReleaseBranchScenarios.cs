﻿using GitTools.Testing;
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
            fixture.BranchTo("develop");
            fixture.Repository.MakeCommits(3);
            var releaseBranch = fixture.Repository.CreateBranch("release/1.0.0");
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.Repository.ApplyTag("1.0.0");
            fixture.Checkout("develop");

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
            fixture.BranchTo("develop");
            fixture.Repository.MakeCommits(3);
            fixture.BranchTo("release/1.0.0");
            fixture.Repository.MakeACommit();

            // Merge to master
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.Repository.ApplyTag("1.0.0");

            // Merge to develop
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.AssertFullSemver("1.1.0-alpha.2");

            fixture.Repository.MakeACommit();
            fixture.Repository.Branches.Remove("release/1.0.0");

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
            fixture.Checkout("release-2.0.0");

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
            fixture.Checkout("releases/2.0.0");

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
            fixture.BranchTo("release-2.0.0");

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
                { "release", new BranchConfig { Tag = "rc" } }
            }
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");

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
            fixture.Checkout("release-2.0.0-Final");

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
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
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
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
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
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
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
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.1.0-alpha.11");
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
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
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
            fixture.Checkout("develop");

            fixture.Repository.MakeCommits(1);
            fixture.AssertFullSemver("1.1.0-alpha.1");

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.1+1");

            //tag it to bump to beta 2
            fixture.Repository.ApplyTag("2.0.0-beta1");

            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.2+2");

            //merge down to develop
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            //but keep working on the release
            fixture.Checkout("release-2.0.0");
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
            fixture.Checkout("develop");

            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver(config, "2.0.0-beta.1");

            //tag it to bump to beta 2
            fixture.Repository.MakeCommits(4);

            fixture.AssertFullSemver(config, "2.0.0-beta.5");

            //merge down to develop
            fixture.Repository.CreateBranch("hotfix-2.0.0");
            fixture.Repository.MakeCommits(2);

            //but keep working on the release
            fixture.Checkout("release-2.0.0");
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
            fixture.Checkout("develop");
            fixture.Repository.MakeACommit();

            fixture.Repository.CreateBranch("release/2.0.0");

            fixture.Repository.CreateBranch("release/2.0.0-xxx");
            fixture.Checkout("release/2.0.0-xxx");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver(config, "2.0.0-beta.1");

            fixture.Checkout("release/2.0.0");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver(config, "2.0.0-beta.1");

            fixture.Repository.MergeNoFF("release/2.0.0-xxx");
            fixture.AssertFullSemver(config, "2.0.0-beta.2");
        }
    }

    [Test]
    public void CommitOnDevelop_AfterReleaseBranchMergeToDevelop_ShouldNotResetCount()
    {
        var config = new Config
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.MakeACommit("initial");
            fixture.BranchTo("develop");

            // Create release from develop
            fixture.BranchTo("release-2.0.0");
            fixture.AssertFullSemver(config, "2.0.0-beta.0");

            // Make some commits on release
            fixture.MakeACommit("release 1");
            fixture.MakeACommit("release 2");
            fixture.AssertFullSemver(config, "2.0.0-beta.2");

            // First forward merge release to develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release-2.0.0");

            // Make some new commit on release
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeACommit("release 3 - after first merge");
            fixture.AssertFullSemver(config, "2.0.0-beta.3");

            // Make new commit on develop
            fixture.Checkout("develop");
            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");
            fixture.AssertFullSemver(config, "2.0.0-beta.3");
            fixture.Checkout("develop");
            fixture.Repository.MakeACommit("develop after merge");

            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");
            fixture.AssertFullSemver(config, "2.0.0-beta.3");

            // Make some new commit on release
            fixture.Repository.MakeACommit("release 4");
            fixture.Repository.MakeACommit("release 5");
            fixture.AssertFullSemver(config, "2.0.0-beta.5");

            // Second merge release to develop
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");
            fixture.AssertFullSemver(config, "2.0.0-beta.5");
        }
    }

    public void ReleaseBranchShouldUseBranchNameVersionDespiteBumpInPreviousCommit()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0");
            fixture.Repository.MakeACommit("+semver:major");
            fixture.Repository.MakeACommit();

            fixture.BranchTo("release/2.0");

            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }
    }

    [Test]
    public void ReleaseBranchWithACommitShouldUseBranchNameVersionDespiteBumpInPreviousCommit()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0");
            fixture.Repository.MakeACommit("+semver:major");
            fixture.Repository.MakeACommit();

            fixture.BranchTo("release/2.0");

            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("2.0.0-beta.1+3");
        }
    }

    [Test]
    public void ReleaseBranchedAtCommitWithSemverMessageShouldUseBranchNameVersion()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0");
            fixture.Repository.MakeACommit("+semver:major");

            fixture.BranchTo("release/2.0");

            fixture.AssertFullSemver("2.0.0-beta.1+1");
        }
    }

    [Test]
    public void FeatureFromReleaseBranch_ShouldNotResetCount()
    {
        var config = new Config
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.CreateBranch("release-2.0.0");
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver(config, "2.0.0-beta.0");

            // Make some commits on release
            fixture.Repository.MakeCommits(10);
            fixture.AssertFullSemver(config, "2.0.0-beta.10");

            // Create feature from release
            fixture.BranchTo("feature/xxx");
            fixture.Repository.MakeACommit("feature 1");
            fixture.Repository.MakeACommit("feature 2");

            // Check version on release
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver(config, "2.0.0-beta.10");
            fixture.Repository.MakeACommit("release 11");
            fixture.AssertFullSemver(config, "2.0.0-beta.11");

            // Make new commit on feature
            Commands.Checkout(fixture.Repository, "feature/xxx");
            fixture.Repository.MakeACommit("feature 3");

            // Checkout to release (no new commits) 
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver(config, "2.0.0-beta.11");

            // Merge feature to release
            fixture.Repository.MergeNoFF("feature/xxx", Generate.SignatureNow());
            fixture.AssertFullSemver(config, "2.0.0-beta.15");

            fixture.Repository.MakeACommit("release 13 - after feature merge");
            fixture.AssertFullSemver(config, "2.0.0-beta.16");
        }
    }
}