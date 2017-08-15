using System.Linq;
using GitTools.Testing;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;
using GitVersion;

[TestFixture]
public class HotfixBranchScenarios
{
    [Test]
    // This test actually validates #465 as well
    public void PatchLatestReleaseExample()
    {
        using (var fixture = new BaseGitFlowRepositoryFixture("1.2.0"))
        {
            // create hotfix
            fixture.Repository.Checkout("master");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("hotfix-1.2.1"));
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.2.1-beta.1+1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+2");
            fixture.Repository.ApplyTag("1.2.1-beta.1");
            fixture.AssertFullSemver("1.2.1-beta.1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.2+3");

            // Merge hotfix branch to master
            fixture.Repository.Checkout("master");

            fixture.Repository.MergeNoFF("hotfix-1.2.1", Generate.SignatureNow());
            fixture.AssertFullSemver("1.2.1+4");

            fixture.Repository.ApplyTag("1.2.1");
            fixture.AssertFullSemver("1.2.1");

            // Verify develop version
            fixture.Repository.Checkout("develop");
            fixture.AssertFullSemver("1.3.0-unstable.1");

            fixture.Repository.MergeNoFF("hotfix-1.2.1", Generate.SignatureNow());
            fixture.AssertFullSemver("1.3.0-unstable.5");
        }
    }

    [Test]
    public void CanTakeVersionFromHotfixesBranch()
    {
        using (var fixture = new BaseGitFlowRepositoryFixture(r =>
        {
            r.MakeATaggedCommit("1.0.0");
            r.MakeATaggedCommit("1.1.0");
            r.MakeATaggedCommit("2.0.0");
        }))
        {

            // Merge hotfix branch to support
            fixture.Repository.Checkout("master");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("support-1.1", (Commit)fixture.Repository.Tags.Single(t => t.FriendlyName == "1.1.0").Target));
            fixture.AssertFullSemver("1.1.0");

            // create hotfix branch
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("hotfixes/1.1.1"));
            fixture.AssertFullSemver("1.1.0"); // We are still on a tagged commit
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.1-beta.1+1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-beta.1+2");
        }
    }

    [Test]
    public void PatchOlderReleaseExample()
    {
        using (var fixture = new BaseGitFlowRepositoryFixture(r =>
        {
            r.MakeATaggedCommit("1.0.0");
            r.MakeATaggedCommit("1.1.0");
            r.MakeATaggedCommit("2.0.0");
        }))
        {
            // Merge hotfix branch to support
            fixture.Repository.Checkout("master");
            var tag = fixture.Repository.Tags.Single(t => t.FriendlyName == "1.1.0");
            var supportBranch = fixture.Repository.CreateBranch("support-1.1", (Commit) tag.Target);
            fixture.Repository.Checkout(supportBranch);
            fixture.AssertFullSemver("1.1.0");

            // create hotfix branch
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("hotfix-1.1.1"));
            fixture.AssertFullSemver("1.1.0"); // We are still on a tagged commit
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.1-beta.1+1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-beta.1+2");

            // Create feature branch off hotfix branch and complete
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/fix"));
            fixture.AssertFullSemver("1.1.1-fix.1+2");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-fix.1+3");

            fixture.Repository.CreatePullRequestRef("feature/fix", "hotfix-1.1.1", normalise: true);
            fixture.AssertFullSemver("1.1.1-PullRequest.2+4");
            fixture.Repository.Checkout("hotfix-1.1.1");
            fixture.Repository.MergeNoFF("feature/fix", Generate.SignatureNow());
            fixture.AssertFullSemver("1.1.1-beta.1+4");

            // Merge hotfix into support branch to complete hotfix
            fixture.Repository.Checkout("support-1.1");
            fixture.Repository.MergeNoFF("hotfix-1.1.1", Generate.SignatureNow());
            fixture.AssertFullSemver("1.1.1+5");
            fixture.Repository.ApplyTag("1.1.1");
            fixture.AssertFullSemver("1.1.1");

            // Verify develop version
            fixture.Repository.Checkout("develop");
            fixture.AssertFullSemver("2.1.0-unstable.1");
            fixture.Repository.MergeNoFF("support-1.1", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.0-unstable.7");
        }
    }

    /// <summary>
    /// Create a feature branch from a hotfix branch, and merge back, then delete it
    /// </summary>
    [Test]
    public void FeatureOnHotfix_FeatureBranchDeleted()
    {
        var config = new Config
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new EmptyRepositoryFixture())
        {

            var release450 = "release/4.5.0";
            var hotfix451 = "hotfix/4.5.1";
            var support45 = "support/4.5";
            var tag450 = "4.5.0";
            var featureBranch = "feature/some-bug-fix";

            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");

            // create release branch
            fixture.Repository.CreateBranch(release450);
            Commands.Checkout(fixture.Repository, release450);
            fixture.AssertFullSemver(config, "4.5.0-beta.0");
            fixture.Repository.MakeACommit("blabla");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MergeNoFF(release450, Generate.SignatureNow());
            Commands.Checkout(fixture.Repository, "master");
            fixture.Repository.MergeNoFF(release450, Generate.SignatureNow());

            // create support branch
            fixture.Repository.CreateBranch(support45);
            Commands.Checkout(fixture.Repository, support45);
            fixture.Repository.ApplyTag(tag450);
            fixture.AssertFullSemver(config, "4.5.0");

            // create hotfix branch
            fixture.Repository.CreateBranch(hotfix451);
            Commands.Checkout(fixture.Repository, hotfix451);

            // feature branch from hotfix
            fixture.Repository.CreateBranch(featureBranch);
            Commands.Checkout(fixture.Repository, featureBranch);
            fixture.Repository.MakeACommit("blabla"); // commit 1
            Commands.Checkout(fixture.Repository, hotfix451);
            fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2
            fixture.Repository.Branches.Remove(featureBranch);
            fixture.AssertFullSemver(config, "4.5.1-beta.2");
        }
    }

    /// <summary>
    /// Create a feature branch from a hotfix branch, and merge back, but don't delete it
    /// </summary>
    [Test]
    public void FeatureOnHotfix_FeatureBranchNotDeleted()
    {
        var config = new Config
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new EmptyRepositoryFixture())
        {

            var release450 = "release/4.5.0";
            var hotfix451 = "hotfix/4.5.1";
            var support45 = "support/4.5";
            var tag450 = "4.5.0";
            var featureBranch = "feature/some-bug-fix";

            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");

            // create release branch
            fixture.Repository.CreateBranch(release450);
            Commands.Checkout(fixture.Repository, release450);
            fixture.AssertFullSemver(config, "4.5.0-beta.0");
            fixture.Repository.MakeACommit("blabla");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MergeNoFF(release450, Generate.SignatureNow());
            Commands.Checkout(fixture.Repository, "master");
            fixture.Repository.MergeNoFF(release450, Generate.SignatureNow());

            // create support branch
            fixture.Repository.CreateBranch(support45);
            Commands.Checkout(fixture.Repository, support45);
            fixture.Repository.ApplyTag(tag450);
            fixture.AssertFullSemver(config, "4.5.0");

            // create hotfix branch
            fixture.Repository.CreateBranch(hotfix451);
            Commands.Checkout(fixture.Repository, hotfix451);

            // feature branch from hotfix
            fixture.Repository.CreateBranch(featureBranch);
            Commands.Checkout(fixture.Repository, featureBranch);
            fixture.Repository.MakeACommit("blabla"); // commit 1
            Commands.Checkout(fixture.Repository, hotfix451);
            fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2
            fixture.AssertFullSemver(config, "4.5.1-beta.2");
        }
    }

}