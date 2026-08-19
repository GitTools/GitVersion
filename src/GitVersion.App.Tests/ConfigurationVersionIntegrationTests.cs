using GitVersion.App.Tests.Helpers;
using GitVersion.Configuration;
using GitVersion.Helpers;

namespace GitVersion.App.Tests;

[TestFixture]
public class ConfigurationVersionIntegrationTests
{
    [Test]
    public void V6AndV7ConfigurationCalculateTheSameVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        var configurationPath = FileSystemHelper.Path.Combine(fixture.RepositoryPath, ConfigurationFileLocator.DefaultFileName);

        FileSystemHelper.File.WriteAllText(configurationPath, "next-version: 2.0.0");
        var v6Result = Execute(fixture.RepositoryPath, "v6");

        FileSystemHelper.File.WriteAllText(configurationPath, """
                                                             calculation:
                                                               next-version: 2.0.0
                                                             output: {}
                                                             """);
        var v7Result = Execute(fixture.RepositoryPath, "v7");

        v6Result.ExitCode.ShouldBe(0);
        v7Result.ExitCode.ShouldBe(0);
        GetFullSemVer(v7Result.Output!).ShouldBe(GetFullSemVer(v6Result.Output!));
    }

    [TestCase("v6", false)]
    [TestCase("v7", true)]
    public void ShowConfigUsesSelectedConfigurationStructure(string version, bool nested)
    {
        using var fixture = new EmptyRepositoryFixture();
        var result = GitVersionHelper.ExecuteIn(
            fixture.RepositoryPath,
            " --show-config",
            logToFile: false,
            new KeyValuePair<string, string?>(ConfigurationVersionSelector.EnvironmentVariableName, version));

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.Contains("calculation:", StringComparison.Ordinal).ShouldBe(nested);
        result.Output.Contains("output:", StringComparison.Ordinal).ShouldBe(nested);
    }

    private static ExecutionResults Execute(string repositoryPath, string version) =>
        GitVersionHelper.ExecuteIn(
            repositoryPath,
            arguments: null,
            logToFile: false,
            new KeyValuePair<string, string?>(ConfigurationVersionSelector.EnvironmentVariableName, version));

    private static string? GetFullSemVer(string json) =>
        JsonDocument.Parse(json).RootElement.GetProperty("FullSemVer").GetString();
}
