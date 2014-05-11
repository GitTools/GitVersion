namespace AcceptanceTests.GitFlow
{
    using GitVersion;
    using Helpers;
    using LibGit2Sharp;
    using Shouldly;
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

                var result = fixture.ExecuteGitVersion();

                result.OutputVariables[VariableProvider.SemVer].ShouldBe("1.1.0.0-unstable");
            }
        }
    }
}