using System.IO;

using GitVersion;

using NUnit.Framework;

using Shouldly;

[TestFixture]
public class ExecCmdLineArgumentTest
{
    const string MsBuild = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";


    [Test]
    public void RunExecViaCommandLine()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.2.3");
            fixture.Repository.MakeACommit();

            var buildFile = Path.Combine(fixture.RepositoryPath, "RunExecViaCommandLine.proj");
            File.Delete(buildFile);
            const string buildFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Target Name=""OutputResults"">
    <Message Text=""GitVersion_FullSemVer: $(GitVersion_FullSemVer)""/>
  </Target>
</Project>";
            File.WriteAllText(buildFile, buildFileContent);
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, MsBuild, "RunExecViaCommandLine.proj /target:OutputResults");

            result.ExitCode.ShouldBe(0);
            result.Log.ShouldContain("GitVersion_FullSemVer: 1.2.4+1");
        }
    }


    [Test]
    public void InvalidArgumentsExitCodeShouldNotBeZero()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.2.3");
            fixture.Repository.MakeACommit();

            var buildFile = Path.Combine(fixture.RepositoryPath, "RunExecViaCommandLine.proj");
            File.Delete(buildFile);
            const string buildFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Target Name=""OutputResults"">
    <Message Text=""GitVersion_FullSemVer: $(GitVersion_FullSemVer)""/>
  </Target>
</Project>";
            File.WriteAllText(buildFile, buildFileContent);
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments : " /invalid-argument");

            result.ExitCode.ShouldBe(1);
            result.Output.ShouldContain("Failed to parse arguments");
        }
    }


    [Test]
    public void UsesGitVersionConfigWhenCreatingDynamicRepository()
    {
        var localRepoPath = PathHelper.GetTempPath();
        var repoBasePath = Path.GetDirectoryName(PathHelper.GetTempPath());
        Directory.CreateDirectory(localRepoPath);

        try
        {
            using (var remote = new EmptyRepositoryFixture(new Config()))
            {
                remote.Repository.MakeACommit();
                var configFile = Path.Combine(localRepoPath, "GitVersionConfig.yaml");
                File.WriteAllText(configFile, "next-version: 1.0.0");

                var arguments = string.Format(" /url {0} /dynamicRepoLocation {1} /b master", remote.RepositoryPath, repoBasePath);
                var results = GitVersionHelper.ExecuteIn(localRepoPath, arguments, false);
                results.OutputVariables.SemVer.ShouldBe("1.0.0");
            }
        }
        finally
        {
            DeleteHelper.DeleteGitRepository(localRepoPath);
            DeleteHelper.DeleteGitRepository(repoBasePath);
        }
    }
}