using System.IO;
using GitTools.Testing;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
    [TestFixture]
    public class MsBuildProjectArgTest
    {
        public const string TestProject = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <Target Name=""OutputResults"">
    <Message Text=""GitVersion_FullSemVer: $(GitVersion_FullSemVer)"" Importance=""High""/>
  </Target>
</Project>
";

        [Test]
        public void RunsMsBuildProvideViaCommandLineArg()
        {
            const string taggedVersion = "1.2.3";
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit(taggedVersion);

            var buildFile = Path.Combine(fixture.RepositoryPath, "RunsMsBuildProvideViaCommandLineArg.proj");
            File.Delete(buildFile);
            File.WriteAllText(buildFile, TestProject);
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, projectFile: "RunsMsBuildProvideViaCommandLineArg.proj", projectArgs: "/target:OutputResults");

            result.ExitCode.ShouldBe(0);
            result.Log.ShouldContain("FullSemVer: 1.2.3");
        }
    }
}
