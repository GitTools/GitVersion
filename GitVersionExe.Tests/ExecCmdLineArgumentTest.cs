using System.IO;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class ExecCmdLineArgumentTest
{
    const string MsBuild = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";

    [Test]
    public void RunExecViaCommandLine()
    {
        using (var fixture = new EmptyRepositoryFixture())
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
}