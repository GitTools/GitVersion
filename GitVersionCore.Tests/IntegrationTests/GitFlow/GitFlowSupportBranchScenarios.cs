using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitFlowSupportBranchScenarios
{
    [Test]
    public void SupportIsCalculatedCorrectly()
    {
        using (var fixture = new BaseGitFlowRepositoryFixture("1.1.0"))
        {
            // Create 2.0.0 release
            var branch = fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout(branch);
            fixture.Repository.MakeCommits(2);

            // Merge into develop and master
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0");
            fixture.Repository.ApplyTag("2.0.0");
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0");
            fixture.AssertFullSemver("2.1.0-unstable.0+0");

            // Now lets support 1.x release
            fixture.Repository.Checkout("1.1.0");
            var supportBranch = fixture.Repository.CreateBranch("support/1.0.0");
            fixture.Repository.Checkout(supportBranch);
            fixture.AssertFullSemver("1.1.0");

            // Create release branch from support branch
            var releaseBranch = fixture.Repository.CreateBranch("release/1.2.0");
            fixture.Repository.Checkout(releaseBranch);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.0-beta.1+1");

            // Create 1.2.0 release
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("release/1.2.0");
            fixture.AssertFullSemver("1.2.0");
            fixture.Repository.ApplyTag("1.2.0");

            // Create 1.2.1 hotfix
            var hotfixBranch = fixture.Repository.CreateBranch("hotfix/1.2.1");
            fixture.Repository.Checkout(hotfixBranch);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+3"); // TODO This should be +1
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("hotfix/1.2.1");
            fixture.AssertFullSemver("1.2.1");
        }
    }
}