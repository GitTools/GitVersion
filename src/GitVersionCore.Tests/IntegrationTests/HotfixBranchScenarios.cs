using System.Linq;
using GitTools.Testing;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

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
            Commands.Checkout(fixture.Repository, "master");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("hotfix-1.2.1"));
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.2.1-beta.1+1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+2");
            fixture.Repository.ApplyTag("1.2.1-beta.1");
            fixture.AssertFullSemver("1.2.1-beta.1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.2+3");

            // Merge hotfix branch to master
            Commands.Checkout(fixture.Repository, "master");

            fixture.Repository.MergeNoFF("hotfix-1.2.1", Generate.SignatureNow());
            fixture.AssertFullSemver("1.2.1+4");

            fixture.Repository.ApplyTag("1.2.1");
            fixture.AssertFullSemver("1.2.1");

            // Verify develop version
            Commands.Checkout(fixture.Repository, "develop");
            fixture.AssertFullSemver("1.3.0-alpha.1");

            fixture.Repository.MergeNoFF("hotfix-1.2.1", Generate.SignatureNow());
            fixture.AssertFullSemver("1.3.0-alpha.5");
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
            Commands.Checkout(fixture.Repository, "master");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("support-1.1", (Commit)fixture.Repository.Tags.Single(t => t.FriendlyName == "1.1.0").Target));
            fixture.AssertFullSemver("1.1.0");

            // create hotfix branch
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("hotfixes/1.1.1"));
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
            Commands.Checkout(fixture.Repository, "master");
            var tag = fixture.Repository.Tags.Single(t => t.FriendlyName == "1.1.0");
            var supportBranch = fixture.Repository.CreateBranch("support-1.1", (Commit) tag.Target);
            Commands.Checkout(fixture.Repository, supportBranch);
            fixture.AssertFullSemver("1.1.0");

            // create hotfix branch
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("hotfix-1.1.1"));
            fixture.AssertFullSemver("1.1.0"); // We are still on a tagged commit
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.1-beta.1+1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-beta.1+2");

            // Create feature branch off hotfix branch and complete
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/fix"));
            fixture.AssertFullSemver("1.1.1-fix.1+2");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-fix.1+3");

            fixture.Repository.CreatePullRequestRef("feature/fix", "hotfix-1.1.1", normalise: true, prNumber: 8);
            fixture.AssertFullSemver("1.1.1-PullRequest0008.4");
            Commands.Checkout(fixture.Repository, "hotfix-1.1.1");
            fixture.Repository.MergeNoFF("feature/fix", Generate.SignatureNow());
            fixture.AssertFullSemver("1.1.1-beta.1+4");

            // Merge hotfix into support branch to complete hotfix
            Commands.Checkout(fixture.Repository, "support-1.1");
            fixture.Repository.MergeNoFF("hotfix-1.1.1", Generate.SignatureNow());
            fixture.AssertFullSemver("1.1.1+5");
            fixture.Repository.ApplyTag("1.1.1");
            fixture.AssertFullSemver("1.1.1");

            // Verify develop version
            Commands.Checkout(fixture.Repository, "develop");
            fixture.AssertFullSemver("2.1.0-alpha.1");
            fixture.Repository.MergeNoFF("support-1.1", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.0-alpha.7");
        }
    }
}