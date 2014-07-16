namespace GitHubFlowVersion.AcceptanceTests
{
    using System.IO;
    using global::AcceptanceTests;
    using global::AcceptanceTests.Helpers;
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

                var buildFile = Path.Combine(fixture.RepositoryPath, "RunsMsBuildProvideViaCommandLineArg.proj");
                File.Delete(buildFile);
                const string buildFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Target Name=""OutputResults"">
    <Message Text=""GitVersion_FullSemVer: $(GitVersion_FullSemVer)""/>
  </Target>
</Project>";
                File.WriteAllText(buildFile, buildFileContent);
                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, projectFile: "RunsMsBuildProvideViaCommandLineArg.proj", projectArgs: "/target:OutputResults");

                result.ExitCode.ShouldBe(0);
                result.Log.ShouldContain("FullSemVer: 1.2.3+0");
            }
        }
    }
}