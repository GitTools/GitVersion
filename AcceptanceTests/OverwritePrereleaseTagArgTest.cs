namespace AcceptanceTests
{
    using GitVersion;
    using Helpers;
    using LibGit2Sharp;
    using Shouldly;
    using Xunit;

    public class OverwritePrereleaseTagArgTest
    {
        [Fact]
        public void NoPrereleaseTag()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit("1.2.0");

                fixture.Repository.CreateBranch("develop").Checkout();
                fixture.Repository.MakeACommit();
                fixture.Repository.CreateBranch("release/1.3");
                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, preReleaseTag: "");
                
                result.OutputVariables[VariableProvider.NuGetVersionV2].ShouldBe("1.3.0");
            }
        }


        [Fact]
        public void CustomPrereleaseTag()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit("1.2.0");

                fixture.Repository.CreateBranch("develop").Checkout();
                fixture.Repository.MakeACommit();
                fixture.Repository.CreateBranch("release/1.3");
                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, preReleaseTag: "myprerealese");

                result.OutputVariables[VariableProvider.NuGetVersionV2].ShouldBe("1.3.0-myprerealese");
            }
        }
    }
}
