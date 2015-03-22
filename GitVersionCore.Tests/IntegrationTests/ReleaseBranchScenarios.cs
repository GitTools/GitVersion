﻿using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class ReleaseBranchScenarios
{
    [Test]
    public void NoMergeBacksToDevelopInCaseThereAreNoChangesInReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeCommits(3);
            var releaseBranch = fixture.Repository.CreateBranch("release/1.0.0");
            releaseBranch.Checkout();
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
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeCommits(3);
            var releaseBranch = fixture.Repository.CreateBranch("release/1.0.0");
            releaseBranch.Checkout();
            fixture.Repository.MakeACommit();

            // Merge to master
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.Repository.ApplyTag("1.0.0");

            // Merge to develop
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release/1.0.0");

            fixture.Repository.MakeACommit();
            fixture.Repository.Branches.Remove(releaseBranch);

            fixture.AssertFullSemver("1.1.0-unstable.1");
        }
    }

    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
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
    public void ReleaseBranchWithNextVersionSetInConfig()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config { NextVersion = "2.0.0"}))
        {
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0").Checkout();

            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }
    }

    [Test]
    public void CanTakeVersionFromReleaseBranchWithTagOverriden()
    {
        var config = new Config();
        config.Branches["release[/-]"].Tag = "rc";
        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-rc.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-rc.1+2");
        }
    }

    [Test]
    public void CanHandleReleaseBranchWithStability()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
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
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0+2");
        }
    }

    [Test]
    public void WhenReleaseBranch_OffMaster_IsMergedIntoMaster_VersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
        }
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoDevelopHighestVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Repository.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-1.0.0", Constants.SignatureNow());

            fixture.AssertFullSemver("2.1.0-unstable.5");
        }
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoMasterHighestVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Repository.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-1.0.0", Constants.SignatureNow());

            fixture.AssertFullSemver("2.0.0+5");
        }
    }

    [Test]
    public void WhenMergingReleaseBackToDevShouldNotResetBetaVersion()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");

            fixture.Repository.MakeCommits(1);

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
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            //but keep working on the release
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.2+3");
        }
    }
}