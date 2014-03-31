namespace AcceptanceTests.GitFlow
{
    using Helpers;
    using LibGit2Sharp;
    using Xunit;

    public class PatchScenarios
    {
        [Fact]
        public void PatchLatestReleaseExample()
        {
            using (var fixture = new BaseGitFlowRepositoryFixture("1.2.0"))
            {
                // create hotfix
                fixture.Repository.CreateBranch("hotfix-1.2.1").Checkout();

                fixture.AssertFullSemver("1.2.1-beta.1+0");
                fixture.Repository.MakeACommit();
                fixture.AssertFullSemver("1.2.1-beta.1+1");
                

                // Merge hotfix branch to master
                fixture.Repository.Checkout("master");
                
                
                // No way to force a merge commit in libgit2, so commit before merge
                fixture.Repository.MergeNoFF("hotfix-1.2.1", Constants.SignatureNow());
                fixture.AssertFullSemver("1.2.1");

                fixture.Repository.ApplyTag("1.2.1");
                fixture.AssertFullSemver("1.2.1");

                // Verify develop version
                fixture.Repository.Checkout("develop");
                fixture.AssertFullSemver("1.3.0.0-unstable");

                fixture.Repository.MergeNoFF("hotfix-1.2.1", Constants.SignatureNow());
                
                //todo: figure out why this is .2 instead of the .1, perhaps the wiki is wrong?
                fixture.AssertFullSemver("1.3.0.2-unstable");
            }
        }
    }
}