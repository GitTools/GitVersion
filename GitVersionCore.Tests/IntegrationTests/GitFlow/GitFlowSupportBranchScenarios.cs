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
            fixture.Repository.CreateBranch("release-2.0.0").Checkout();
            fixture.Repository.MakeCommits(2);

            // Merge into develop and master
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0");
            fixture.Repository.ApplyTag("2.0.0");
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0");
            fixture.AssertFullSemver("2.1.0-unstable.1+1");

            // Now lets support 1.x release
            fixture.Repository.Checkout("1.1.0");
            fixture.Repository.CreateBranch("support/1.0.0").Checkout();
            fixture.AssertFullSemver("1.1.0");

            // Create release branch from support branch
            fixture.Repository.CreateBranch("release/1.2.0").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.0-beta.1+1");

            // Create 1.2.0 release
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("release/1.2.0");
            fixture.AssertFullSemver("1.2.0");
            fixture.Repository.ApplyTag("1.2.0");

            // Create 1.2.1 hotfix
            fixture.Repository.CreateBranch("hotfix/1.2.1").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+3"); // TODO This should be +1
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("hotfix/1.2.1");
            fixture.AssertFullSemver("1.2.1");
        }
    }
}