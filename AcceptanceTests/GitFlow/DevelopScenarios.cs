namespace AcceptanceTests.GitFlow
{
    using Helpers;
    using LibGit2Sharp;
    using Xunit;

    public class DevelopScenarios
    {
        [Fact]
        public void WhenDevelopBranchedFromMaster_MinorIsIncreased()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit("1.0.0");
                fixture.Repository.CreateBranch("develop").Checkout();
                fixture.AssertFullSemver("1.1.0-unstable.0+0");
            }
        }

        [Fact]
        public void WhenDevelopBranchedFromMasterDetachedHead_MinorIsIncreased()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit("1.0.0");
                fixture.Repository.CreateBranch("develop").Checkout();
                fixture.Repository.MakeACommit();
                var commit = fixture.Repository.Head.Tip;
                fixture.Repository.MakeACommit();
                fixture.Repository.Checkout(commit);
                fixture.AssertFullSemver("1.1.0-unstable.1+1");
            }
        }
    }
}