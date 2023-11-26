using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class HotfixBranchScenarios : TestBase
{
    [Test]
    // This test actually validates #465 as well
    public void PatchLatestReleaseExample()
    {
        using var fixture = new BaseGitFlowRepositoryFixture("1.2.0");
        // create hotfix
        Commands.Checkout(fixture.Repository, MainBranch);
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("hotfix-1.2.1"));
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("1.2.1-beta.1+1");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.2.1-beta.1+2");
        fixture.Repository.ApplyTag("1.2.1-beta.1");
        fixture.AssertFullSemver("1.2.1-beta.1");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.2.1-beta.2+1");

        // Merge hotfix branch to main
        Commands.Checkout(fixture.Repository, MainBranch);

        fixture.Repository.MergeNoFF("hotfix-1.2.1", Generate.SignatureNow());
        fixture.AssertFullSemver("1.2.1-4");

        fixture.Repository.ApplyTag("1.2.1");
        fixture.AssertFullSemver("1.2.1");

        // Verify develop version
        Commands.Checkout(fixture.Repository, "develop");
        fixture.AssertFullSemver("1.3.0-alpha.1");

        fixture.Repository.MergeNoFF("hotfix-1.2.1", Generate.SignatureNow());
        fixture.AssertFullSemver("1.3.0-alpha.5");
    }

    [Test]
    public void CanTakeVersionFromHotfixesBranch()
    {
        using var fixture = new BaseGitFlowRepositoryFixture(r =>
        {
            r.MakeATaggedCommit("1.0.0");
            r.MakeATaggedCommit("1.1.0");
            r.MakeATaggedCommit("2.0.0");
        });
        // Merge hotfix branch to support
        var branch = fixture.Repository.CreateBranch(
            "support-1.1", (LibGit2Sharp.Commit)fixture.Repository.Tags.Single(t => t.FriendlyName == "1.1.0").Target
        );
        Commands.Checkout(fixture.Repository, branch);
        fixture.AssertFullSemver("1.1.0");

        // create hotfix branch
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("hotfixes/1.1.1"));
        fixture.AssertFullSemver("1.1.1-beta.1+0");
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("1.1.1-beta.1+1");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.1.1-beta.1+2");
    }

    [Test]
    public void PatchOlderReleaseExample()
    {
        using var fixture = new BaseGitFlowRepositoryFixture(r =>
        {
            r.MakeATaggedCommit("1.0.0");
            r.MakeATaggedCommit("1.1.0");
            r.MakeATaggedCommit("2.0.0");
        });
        // Merge hotfix branch to support
        fixture.Checkout(MainBranch);
        var tag = fixture.Repository.Tags.Single(t => t.FriendlyName == "1.1.0");
        fixture.Repository.CreateBranch("support-1.1", (LibGit2Sharp.Commit)tag.Target);
        fixture.Checkout("support-1.1");
        fixture.AssertFullSemver("1.1.0");

        // create hotfix branch
        fixture.BranchTo("hotfix-1.1.1");
        fixture.AssertFullSemver("1.1.1-beta.1+0");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.1.1-beta.1+1");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1-beta.1+2");

        // Create feature branch off hotfix branch and complete
        fixture.BranchTo("feature/fix");
        fixture.AssertFullSemver("1.1.1-fix.1+2");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1-fix.1+3");

        fixture.Repository.CreatePullRequestRef("feature/fix", "hotfix-1.1.1", prNumber: 8, normalise: true);
        fixture.AssertFullSemver("1.1.1-PullRequest8.4");
        fixture.Checkout("hotfix-1.1.1");
        fixture.MergeNoFF("feature/fix");
        fixture.AssertFullSemver("1.1.1-beta.1+4");

        // Merge hotfix into support branch to complete hotfix
        fixture.Checkout("support-1.1");
        fixture.MergeNoFF("hotfix-1.1.1");
        fixture.AssertFullSemver("1.1.1-5");
        fixture.ApplyTag("1.1.1");
        fixture.AssertFullSemver("1.1.1");

        // Verify develop version
        fixture.Checkout("develop");
        fixture.AssertFullSemver("2.1.0-alpha.1");
        fixture.MergeNoFF("support-1.1");
        fixture.AssertFullSemver("2.1.0-alpha.7");
    }

    /// <summary>
    /// Create a feature branch from a hotfix branch, and merge back, then delete it
    /// </summary>
    [Test]
    public void FeatureOnHotfixFeatureBranchDeleted()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatchTag)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        const string release450 = "release/4.5.0";
        const string hotfix451 = "hotfix/4.5.1";
        const string support45 = "support/4.5";
        const string tag450 = "4.5.0";
        const string featureBranch = "feature/some-bug-fix";

        fixture.MakeACommit("initial");
        fixture.BranchTo("develop");

        // create release branch
        fixture.BranchTo(release450);
        fixture.AssertFullSemver("4.5.0-beta.1+0", configuration);
        fixture.MakeACommit("blabla");
        fixture.Checkout("develop");
        fixture.MergeNoFF(release450);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(release450);

        // create support branch
        fixture.BranchTo(support45);
        fixture.ApplyTag(tag450);
        fixture.AssertFullSemver("4.5.0", configuration);

        // create hotfix branch
        fixture.BranchTo(hotfix451);

        // feature branch from hotfix
        fixture.BranchTo(featureBranch);
        fixture.MakeACommit("blabla"); // commit 1
        fixture.Checkout(hotfix451);
        fixture.MergeNoFF(featureBranch); // commit 2
        fixture.Repository.Branches.Remove(featureBranch);
        fixture.AssertFullSemver("4.5.1-beta.1+3", configuration);
    }

    /// <summary>
    /// Create a feature branch from a hotfix branch, and merge back, but don't delete it
    /// </summary>
    [Test]
    public void FeatureOnHotfixFeatureBranchNotDeleted()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatchTag)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        const string release450 = "release/4.5.0";
        const string hotfix451 = "hotfix/4.5.1";
        const string support45 = "support/4.5";
        const string tag450 = "4.5.0";
        const string featureBranch = "feature/some-bug-fix";

        fixture.MakeACommit("initial");
        fixture.BranchTo("develop");

        // create release branch
        fixture.BranchTo(release450);
        fixture.AssertFullSemver("4.5.0-beta.1+0", configuration);
        fixture.MakeACommit("blabla");
        fixture.Checkout("develop");
        fixture.MergeNoFF(release450);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(release450);

        // create support branch
        fixture.BranchTo(support45);
        fixture.ApplyTag(tag450);
        fixture.AssertFullSemver("4.5.0", configuration);

        // create hotfix branch
        fixture.BranchTo(hotfix451);

        // feature branch from hotfix
        fixture.BranchTo(featureBranch);
        fixture.MakeACommit("blabla"); // commit 1
        fixture.Checkout(hotfix451);
        fixture.MergeNoFF(featureBranch); // commit 2

        fixture.AssertFullSemver("4.5.1-beta.1+3", configuration);
    }

    [Test]
    public void IsVersionTakenFromHotfixBranchName()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new BaseGitFlowRepositoryFixture("4.20.4");

        fixture.Checkout("develop");
        fixture.AssertFullSemver("4.21.0-alpha.1", configuration);

        fixture.BranchTo("release/4.21.1");
        fixture.AssertFullSemver("4.21.1-beta.1+0", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("4.21.1-beta.1+1", configuration);

        fixture.BranchTo("hotfix/4.21.1");
        fixture.AssertFullSemver("4.21.1-beta.1+1", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("4.21.1-beta.1+2", configuration);
    }
}
