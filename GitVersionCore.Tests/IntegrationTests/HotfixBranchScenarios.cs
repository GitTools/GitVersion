using System.Linq;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class HotfixBranchScenarios
{
    [Test]
    public void PatchLatestReleaseExample()
    {
        using (var fixture = new BaseGitFlowRepositoryFixture("1.2.0"))
        {
            // create hotfix
            fixture.Repository.CreateBranch("hotfix-1.2.1").Checkout();

            fixture.AssertFullSemver("1.2.1-beta.1+0");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+1");
            fixture.Repository.ApplyTag("1.2.1-beta.1");
            fixture.AssertFullSemver("1.2.1-beta.1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.2+2");

            // Merge hotfix branch to master
            fixture.Repository.Checkout("master");


            fixture.Repository.MergeNoFF("hotfix-1.2.1", Constants.SignatureNow());
            fixture.AssertFullSemver("1.2.1+0");

            fixture.Repository.ApplyTag("1.2.1");
            fixture.AssertFullSemver("1.2.1");

            // Verify develop version
            fixture.Repository.Checkout("develop");
            fixture.AssertFullSemver("1.3.0-unstable.1");

            fixture.Repository.MergeNoFF("hotfix-1.2.1", Constants.SignatureNow());
            fixture.AssertFullSemver("1.3.0-unstable.0");
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
            fixture.Repository.CreateBranch("support-1.1", (Commit)fixture.Repository.Tags.Single(t => t.Name == "1.1.0").Target).Checkout();
            fixture.AssertFullSemver("1.1.0");

            // create hotfix branch
            fixture.Repository.CreateBranch("hotfix-1.1.1").Checkout();
            fixture.AssertFullSemver("1.1.0"); // We are still on a tagged commit
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.1-beta.1+1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-beta.1+2");

            // Create feature branch off hotfix branch and complete
            fixture.Repository.CreateBranch("feature/fix").Checkout();
            fixture.AssertFullSemver("1.1.1-fix.1+2");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-fix.1+3");

            fixture.Repository.CreatePullRequest("feature/fix", "hotfix-1.1.1", isRemotePr: false);
            fixture.AssertFullSemver("1.1.1-PullRequest.2+4");
            fixture.Repository.Checkout("hotfix-1.1.1");
            fixture.Repository.MergeNoFF("feature/fix", Constants.SignatureNow());
            fixture.AssertFullSemver("1.1.1-beta.1+1");

            // Merge hotfix into support branch to complete hotfix
            fixture.Repository.Checkout("support-1.1");
            fixture.Repository.MergeNoFF("hotfix-1.1.1", Constants.SignatureNow());
            fixture.AssertFullSemver("1.1.1+0");
            fixture.Repository.ApplyTag("1.1.1");
            fixture.AssertFullSemver("1.1.1");

            // Verify develop version
            fixture.Repository.Checkout("develop");
            fixture.AssertFullSemver("2.1.0-unstable.1");
            fixture.Repository.MergeNoFF("support-1.1", Constants.SignatureNow());
            fixture.AssertFullSemver("2.1.0-unstable.7");
        }
    }
}