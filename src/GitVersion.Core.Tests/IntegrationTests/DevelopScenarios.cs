using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class DevelopScenarios : TestBase
{
    [Test]
    public void WhenDevelopHasMultipleCommitsSpecifyExistingCommitId()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));

        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        var thirdCommit = fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("1.1.0-alpha.3", commitId: thirdCommit.Sha);
    }

    [Test]
    public void WhenDevelopHasMultipleCommitsSpecifyNonExistingCommitId()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));

        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("1.1.0-alpha.5", commitId: "non-existing-commit-id");
    }

    [Test]
    public void WhenDevelopBranchedFromTaggedCommitOnMainVersionDoesNotChange()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.AssertFullSemver("1.0.0");
    }

    [Test]
    public void CanChangeDevelopTagViaConfig()
    {
        var configuration = new GitVersionConfiguration
        {
            Branches =
            {
                {
                    "develop",
                    new BranchConfiguration
                    {
                        Tag = "alpha",
                        SourceBranches = new HashSet<string>()
                    }
                }
            }
        };
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);
    }

    [Test]
    public void WhenDeveloperBranchExistsDontTreatAsDevelop()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("developer"));
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.0.1-developer.1+1"); // this tag should be the branch name by default, not unstable
    }

    [Test]
    public void WhenDevelopBranchedFromMainMinorIsIncreased()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1");
    }

    [Test]
    public void MergingReleaseBranchBackIntoDevelopWithMergingToMainDoesBumpDevelopVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("release-2.0.0"));
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, MainBranch);
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());
        fixture.AssertFullSemver("2.1.0-alpha.2");
    }

    [Test]
    public void CanHandleContinuousDelivery()
    {
        var configuration = new GitVersionConfiguration
        {
            Branches = { { "develop", new BranchConfiguration { VersioningMode = VersioningMode.ContinuousDelivery } } }
        };
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeATaggedCommit("1.1.0-alpha7");
        fixture.AssertFullSemver("1.1.0-alpha.7", configuration);
    }

    [Test]
    public void WhenDevelopBranchedFromMainDetachedHeadMinorIsIncreased()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        var commit = fixture.Repository.Head.Tip;
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, commit);
        fixture.AssertFullSemver("1.1.0-alpha.1", onlyTrackedBranches: false);
    }

    [Test]
    public void InheritVersionFromParentReleaseBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
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
        fixture.AssertFullSemver("2.1.0-MyFeature.1+5");
    }

    [Test]
    public void InheritVersionFromParentReleaseBranchWithVersion2InsteadOfVersion3()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.Repository.CreateBranch("release/3.0.0");
        fixture.MakeACommit();
        fixture.BranchTo("release/2.0.0");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.1+2");
        fixture.Checkout("develop");
        fixture.AssertFullSemver("3.1.0-alpha.1");
        fixture.MakeACommit();
        fixture.AssertFullSemver("3.1.0-alpha.2");
        fixture.MergeNoFF("release/2.0.0");
        fixture.AssertFullSemver("3.1.0-alpha.5");
        fixture.Checkout("release/2.0.0");
        fixture.BranchTo("feature/MyFeature");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-MyFeature.1+3");
    }

    [Test]
    public void WhenMultipleDevelopBranchesExistAndCurrentBranchHasIncrementInheritPolicyAndCurrentCommitIsAMerge()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("bob_develop");
        fixture.Repository.CreateBranch("develop");
        fixture.Repository.CreateBranch("feature/x");

        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.MakeACommit();

        Commands.Checkout(fixture.Repository, "feature/x");
        fixture.Repository.MakeACommit();
        fixture.Repository.MergeNoFF("develop");

        fixture.AssertFullSemver("1.1.0-x.1+3");
    }

    [Test]
    public void TagOnHotfixShouldNotAffectDevelop()
    {
        using var fixture = new BaseGitFlowRepositoryFixture("1.2.0");
        Commands.Checkout(fixture.Repository, MainBranch);
        var hotfix = fixture.Repository.CreateBranch("hotfix-1.2.1");
        Commands.Checkout(fixture.Repository, hotfix);
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.2.1-beta.1+1");
        fixture.Repository.ApplyTag("1.2.1-beta.1");
        fixture.AssertFullSemver("1.2.1-beta.1");
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.3.0-alpha.2");
    }

    [Test]
    public void CommitsSinceVersionSourceShouldNotGoDownUponGitFlowReleaseFinish()
    {
        var configuration = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.1.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit("commit in develop - 1");
        fixture.AssertFullSemver("1.2.0-alpha.1");
        fixture.BranchTo("release/1.2.0");
        fixture.AssertFullSemver("1.2.0-beta.1+0");
        fixture.Checkout("develop");
        fixture.MakeACommit("commit in develop - 2");
        fixture.MakeACommit("commit in develop - 3");
        fixture.MakeACommit("commit in develop - 4");
        fixture.MakeACommit("commit in develop - 5");
        fixture.AssertFullSemver("1.3.0-alpha.4");
        fixture.Checkout("release/1.2.0");
        fixture.MakeACommit("commit in release/1.2.0 - 1");
        fixture.MakeACommit("commit in release/1.2.0 - 2");
        fixture.MakeACommit("commit in release/1.2.0 - 3");
        fixture.AssertFullSemver("1.2.0-beta.1+3");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("release/1.2.0");
        fixture.ApplyTag("1.2.0");
        fixture.Checkout("develop");
        fixture.MergeNoFF("release/1.2.0");
        fixture.MakeACommit("commit in develop - 6");
        fixture.AssertFullSemver("1.3.0-alpha.9");
        fixture.SequenceDiagram.Destroy("release/1.2.0");
        fixture.Repository.Branches.Remove("release/1.2.0");

        const string expectedFullSemVer = "1.3.0-alpha.9";
        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }

    [Test]
    public void CommitsSinceVersionSourceShouldNotGoDownUponMergingFeatureOnlyToDevelop()
    {
        var configuration = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit($"commit in {MainBranch} - 1");
        fixture.ApplyTag("1.1.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit("commit in develop - 1");
        fixture.AssertFullSemver("1.2.0-alpha.1");
        fixture.BranchTo("release/1.2.0");
        fixture.MakeACommit("commit in release - 1");
        fixture.MakeACommit("commit in release - 2");
        fixture.MakeACommit("commit in release - 3");
        fixture.AssertFullSemver("1.2.0-beta.1+3");
        fixture.ApplyTag("1.2.0");
        fixture.Checkout("develop");
        fixture.MakeACommit("commit in develop - 2");
        fixture.AssertFullSemver("1.3.0-alpha.1");
        fixture.MergeNoFF("release/1.2.0");
        fixture.AssertFullSemver("1.3.0-alpha.5");
        fixture.SequenceDiagram.Destroy("release/1.2.0");
        fixture.Repository.Branches.Remove("release/1.2.0");

        const string expectedFullSemVer = "1.3.0-alpha.5";
        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }

    [Test]
    public void PreviousPreReleaseTagShouldBeRespectedWhenCountingCommits()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.Repository.MakeACommit();

        fixture.BranchTo("develop");
        fixture.MakeATaggedCommit("1.0.0-alpha.3"); // manual bump version

        fixture.MakeACommit();
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.0-alpha.5");
    }

    [Test]
    public void WhenPreventIncrementOfMergedBranchVersionIsSetToFalseForDevelopCommitsSinceVersionSourceShouldNotGoDownWhenMergingReleaseToDevelop()
    {
        var configuration = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                { "develop", new BranchConfiguration { PreventIncrementOfMergedBranchVersion = false } }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        const string ReleaseBranch = "release/1.1.0";
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(1);

        // Create a release branch and make some commits
        fixture.BranchTo(ReleaseBranch);
        fixture.Repository.MakeCommits(3);

        // Simulate a GitFlow release finish.
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(ReleaseBranch);
        fixture.ApplyTag("v1.1.0");
        fixture.Checkout("develop");
        // Simulate some work done on develop while the release branch was open.
        fixture.Repository.MakeCommits(2);
        fixture.MergeNoFF(ReleaseBranch);

        // Version numbers will still be correct when the release branch is around.
        fixture.AssertFullSemver("1.2.0-alpha.6");
        fixture.AssertFullSemver("1.2.0-alpha.6", configuration);

        var versionSourceBeforeReleaseBranchIsRemoved = fixture.GetVersion(configuration).Sha;

        fixture.Repository.Branches.Remove(ReleaseBranch);
        var versionSourceAfterReleaseBranchIsRemoved = fixture.GetVersion(configuration).Sha;
        Assert.AreEqual(versionSourceBeforeReleaseBranchIsRemoved, versionSourceAfterReleaseBranchIsRemoved);
        fixture.AssertFullSemver("1.2.0-alpha.6");
        fixture.AssertFullSemver("1.2.0-alpha.6", configuration);
    }

    [Test]
    public void WhenPreventIncrementOfMergedBranchVersionIsSetToTrueForDevelopCommitsSinceVersionSourceShouldNotGoDownWhenMergingReleaseToDevelop()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithBranch("develop", builder => builder
                .WithPreventIncrementOfMergedBranchVersion(true)
            )
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        const string ReleaseBranch = "release/1.1.0";
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(1);

        // Create a release branch and make some commits
        fixture.BranchTo(ReleaseBranch);
        fixture.Repository.MakeCommits(3);

        // Simulate a GitFlow release finish.
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(ReleaseBranch);
        fixture.ApplyTag("v1.1.0");
        fixture.Checkout("develop");
        // Simulate some work done on develop while the release branch was open.
        fixture.Repository.MakeCommits(2);
        fixture.MergeNoFF(ReleaseBranch);

        // Version numbers will still be correct when the release branch is around.
        fixture.AssertFullSemver("1.2.0-alpha.6");
        fixture.AssertFullSemver("1.2.0-alpha.6", configuration);

        var versionSourceBeforeReleaseBranchIsRemoved = fixture.GetVersion(configuration).Sha;

        fixture.Repository.Branches.Remove(ReleaseBranch);
        var versionSourceAfterReleaseBranchIsRemoved = fixture.GetVersion(configuration).Sha;
        Assert.AreEqual(versionSourceBeforeReleaseBranchIsRemoved, versionSourceAfterReleaseBranchIsRemoved);
        fixture.AssertFullSemver("1.2.0-alpha.6");
        fixture.AssertFullSemver("1.2.0-alpha.3", configuration);
    }

    [Test]
    public void WhenPreventIncrementOfMergedBranchVersionIsSetToFalseForDevelopCommitsSinceVersionSourceShouldNotGoDownWhenMergingHotfixToDevelop()
    {
        var configuration = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                { "develop", new BranchConfiguration { PreventIncrementOfMergedBranchVersion = false } },
                { "hotfix", new BranchConfiguration { PreventIncrementOfMergedBranchVersion = true, Regex = "^(origin/)?hotfix[/-]" } }

            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(1);

        fixture.Checkout("develop");
        fixture.Repository.MakeCommits(3);
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);

        const string ReleaseBranch = "release/1.1.0";
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch(ReleaseBranch));
        fixture.Repository.MakeCommits(3);
        fixture.AssertFullSemver("1.1.0-beta.3", configuration);

        // Simulate a GitFlow release finish.
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(ReleaseBranch);
        fixture.ApplyTag("v1.1.0");
        fixture.Checkout("develop");

        // Simulate some work done on develop while the release branch was open.
        fixture.Repository.MakeCommits(2);
        fixture.MergeNoFF(ReleaseBranch);
        fixture.Repository.Branches.Remove(ReleaseBranch);
        fixture.AssertFullSemver("1.2.0-alpha.6", configuration);

        // Create hotfix for defects found in release/1.1.0
        const string HotfixBranch = "hotfix/1.1.1";
        fixture.Checkout(MainBranch);
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch(HotfixBranch));
        fixture.Repository.MakeCommits(3);

        // Hotfix finish
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF(HotfixBranch);
        fixture.Repository.ApplyTag("v1.1.1");

        // Verify develop version
        fixture.Checkout("develop");
        // Simulate some work done on develop while the hotfix branch was open.
        fixture.Repository.MakeCommits(3);
        fixture.AssertFullSemver("1.2.0-alpha.9", configuration);
        fixture.Repository.MergeNoFF(HotfixBranch);
        fixture.AssertFullSemver("1.2.0-alpha.19", configuration);

        fixture.Repository.Branches.Remove(HotfixBranch);
        fixture.AssertFullSemver("1.2.0-alpha.19", configuration);
    }

    [Test]
    public void NextVersionShouldBeConsideredOnTheMainBranch()
    {
        using EmptyRepositoryFixture fixture = new("main");

        var configurationBuilder = GitFlowConfigurationBuilder.New;

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1+1", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0+1", configurationBuilder.Build());

        fixture.MakeACommit();
        configurationBuilder.WithNextVersion(null);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1+2", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0+2", configurationBuilder.Build());
    }

    /// <summary>
    /// Prevent decrementation of versions on the develop branch #3177
    /// (see https://github.com/GitTools/GitVersion/discussions/3177)
    /// </summary>
    [Test]
    public void PreventDecrementationOfVersionsOnTheMainBranch()
    {
        using EmptyRepositoryFixture fixture = new("develop");

        var configurationBuilder = GitFlowConfigurationBuilder.New;

        fixture.MakeACommit();
        configurationBuilder.WithNextVersion("1.0.0");

        // now we are ready to start with the preparation of the 1.0.0 release
        fixture.BranchTo("release/1.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configurationBuilder.Build());

        // now we makes changes on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configurationBuilder.Build());

        // now we do the actual release of beta 1
        fixture.Checkout("release/1.0.0");
        fixture.ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1", configurationBuilder.Build());

        // continue with more work on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.2", configurationBuilder.Build());

        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.4", configurationBuilder.Build());

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.4", configurationBuilder.Build());

        fixture.Repository.Tags.Remove("1.0.0-beta.1");

        // Merge from develop to main
        fixture.BranchTo("main");

        // ❔ expected: "0.0.1+4"
        // This behavior needs to be changed for the git flow workflow using the track-merge-message or track-merge-target options.
        // [Bug] track-merge-changes produces unexpected result when combining hotfix and support branches #3052
        fixture.AssertFullSemver("1.0.0+0", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0+0", configurationBuilder.Build());

        // Mark this version as RTM
        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configurationBuilder.Build());
    }
}
