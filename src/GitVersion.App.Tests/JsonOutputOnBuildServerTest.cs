using GitVersion.Agents;
using GitVersion.Core.Tests;
using GitVersion.Helpers;

namespace GitVersion.App.Tests;

public class JsonOutputOnBuildServerTest
{
    [Test]
    public void BeingOnBuildServerDoesntOverrideOutputJson()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.2.3");
        fixture.Repository.MakeACommit();

        var env = new KeyValuePair<string, string?>(TeamCity.EnvironmentVariableName, "8.0.0");

        var result = GitVersionHelper.ExecuteIn(fixture.LocalRepositoryFixture.RepositoryPath, arguments: " /output json", environments: env);

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldStartWith("{");
        result.Output.TrimEnd().ShouldEndWith("}");
    }

    [Test]
    public void BeingOnBuildServerWithOutputJsonDoesNotFail()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.2.3");
        fixture.Repository.MakeACommit();

        var env = new KeyValuePair<string, string?>(TeamCity.EnvironmentVariableName, "8.0.0");

        var result = GitVersionHelper.ExecuteIn(fixture.LocalRepositoryFixture.RepositoryPath, arguments: " /output json /output buildserver", environments: env);

        result.ExitCode.ShouldBe(0);
        const string expectedVersion = "0.0.1-5";
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain($"##teamcity[buildNumber '{expectedVersion}']");
        result.OutputVariables.ShouldNotBeNull();
        result.OutputVariables.FullSemVer.ShouldBeEquivalentTo(expectedVersion);
    }

    [TestCase("", "GitVersion.json")]
    [TestCase("version.json", "version.json")]
    public void BeingOnBuildServerWithOutputJsonAndOutputFileDoesNotFail(string outputFile, string fileName)
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.2.3");
        fixture.Repository.MakeACommit();

        var env = new KeyValuePair<string, string?>(TeamCity.EnvironmentVariableName, "8.0.0");

        var result = GitVersionHelper.ExecuteIn(fixture.LocalRepositoryFixture.RepositoryPath, arguments: $" /output json /output buildserver /output file /outputfile {outputFile}", environments: env);

        result.ExitCode.ShouldBe(0);
        const string expectedVersion = "0.0.1-5";
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain($"##teamcity[buildNumber '{expectedVersion}']");
        result.OutputVariables.ShouldNotBeNull();
        result.OutputVariables.FullSemVer.ShouldBeEquivalentTo(expectedVersion);

        var filePath = FileSystemHelper.Path.Combine(fixture.LocalRepositoryFixture.RepositoryPath, fileName);
        var json = FileSystemHelper.File.ReadAllText(filePath);

        var outputVariables = json.ToGitVersionVariables();
        outputVariables.ShouldNotBeNull();
        outputVariables.FullSemVer.ShouldBeEquivalentTo(expectedVersion);
    }
}
