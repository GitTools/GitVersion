namespace GitHubFlowVersion.AcceptanceTests
{
    using System.IO;
    using global::AcceptanceTests;
    using global::AcceptanceTests.Helpers;
    using global::AcceptanceTests.Properties;
    using Shouldly;
    using Xunit;

    public class MsBuildProjectArgTest
    {
        [Fact]
        public void RunsMsBuildProvideViaCommandLineArg()
        {
            const string TaggedVersion = "1.2.3";
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit(TaggedVersion);

                var buildFile = Path.Combine(fixture.RepositoryPath, "TestBuildFile.proj");
                File.WriteAllBytes(buildFile, Resources.TestBuildFile);
                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, projectFile: "TestBuildFile.proj", projectArgs: "/target:OutputResults");

                result.ExitCode.ShouldBe(0);
                result.Log.ShouldContain("FullSemVer: 1.2.3+0");
            }
        }
    }
}