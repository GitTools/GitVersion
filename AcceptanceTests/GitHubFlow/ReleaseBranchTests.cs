namespace GitHubFlowVersion.AcceptanceTests
{
    using GitVersion;
    using global::AcceptanceTests;
    using global::AcceptanceTests.Helpers;
    using LibGit2Sharp;
    using Shouldly;
    using Xunit;

    public class ReleaseBranchTests
    {
        [Fact]
        public void CanTakeVersionFromReleaseBranch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string TaggedVersion = "1.0.3";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.MakeCommits(5);
                fixture.Repository.CreateBranch("release-2.0.0");
                fixture.Repository.Checkout("release-2.0.0");

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("2.0.0-beta.1+5");
            }
        }

        [Fact]
        public void WhenReleaseBranchIsMergedIntoMasterVersionIsTakenWithIt()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string TaggedVersion = "1.0.3";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.MakeCommits(1);
                fixture.Repository.CreateBranch("release-2.0.0");
                fixture.Repository.Checkout("release-2.0.0");
                fixture.Repository.MakeCommits(4);
                fixture.Repository.Checkout("master");
                fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("2.0.0+6");
            }
        }
    }
}
