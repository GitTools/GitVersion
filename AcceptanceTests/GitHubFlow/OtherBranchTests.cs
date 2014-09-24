namespace GitHubFlowVersion.AcceptanceTests
{
    using global::AcceptanceTests;
    using global::AcceptanceTests.Helpers;
    using LibGit2Sharp;
    using Xunit;

    public class OtherBranchTests
    {
        [Fact]
        public void CanTakeVersionFromReleaseBranch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string TaggedVersion = "1.0.3";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.MakeCommits(5);
                fixture.Repository.CreateBranch("alpha-2.0.0");
                fixture.Repository.Checkout("alpha-2.0.0");

                fixture.AssertFullSemver("2.0.0-alpha.1+5");
            }
        }
    }
}