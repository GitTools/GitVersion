using GitVersion.App.Tests.Helpers;
using GitVersion.Configuration;
using GitVersion.Helpers;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationVersionIntegrationTests
{
    [Test]
    public async Task ConfigMigrateWritesMigratedConfigurationToStandardOutput()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var configurationPath = Path.Combine(directory.FullName, ConfigurationFileLocator.DefaultFileName);
            await File.WriteAllTextAsync(configurationPath, "next-version: 2.0.0");

            var result = await new ProgramFixture(directory.FullName).Run("config", "migrate");

            result.ExitCode.ShouldBe(0);
            result.Output.ShouldNotBeNull();
            result.Output.ShouldContain("calculation:");
            result.Output.ShouldContain("next-version: 2.0.0");
            result.Output.ShouldContain("output: {}");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ConfigMigrateDoesNotOverwriteOutputWithoutForce()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var configurationPath = Path.Combine(directory.FullName, ConfigurationFileLocator.DefaultFileName);
            var outputPath = Path.Combine(directory.FullName, "GitVersion.v7.yml");
            await File.WriteAllTextAsync(configurationPath, "next-version: 2.0.0");
            await File.WriteAllTextAsync(outputPath, "existing configuration");

            var result = await new ProgramFixture(directory.FullName).Run("config", "migrate", "--output", "GitVersion.v7.yml");

            result.ExitCode.ShouldBe(1);
            result.Output.ShouldBeEmpty();
            var output = await File.ReadAllTextAsync(outputPath);
            output.ShouldBe("existing configuration");

            var forceResult = GitVersionHelper.ExecuteIn(
                directory.FullName,
                " config migrate --output GitVersion.v7.yml --force",
                logToFile: false);

            forceResult.ExitCode.ShouldBe(0);
            forceResult.Output.ShouldNotBeNull();
            forceResult.Output.ShouldContain("Comments cannot be preserved during migration.");
            var forcedOutput = await File.ReadAllTextAsync(outputPath);
            forcedOutput.ShouldContain("calculation:");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ConfigMigrateInPlaceMigratesExplicitConfigurationOutsideGitRepository()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            const string fileName = "legacy.yml";
            var configurationPath = Path.Combine(directory.FullName, fileName);
            await File.WriteAllTextAsync(configurationPath, "next-version: 2.0.0");

            var result = GitVersionHelper.ExecuteIn(
                directory.FullName,
                $" config migrate --config {fileName} --in-place",
                logToFile: false);

            result.ExitCode.ShouldBe(0);
            result.Output.ShouldNotBeNull();
            result.Output.ShouldContain("Comments cannot be preserved during migration.");
            var migratedConfiguration = await File.ReadAllTextAsync(configurationPath);
            migratedConfiguration.ShouldContain("calculation:");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ConfigMigrateInPlaceUsesPositionalTargetPath()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var configurationPath = Path.Combine(directory.FullName, ConfigurationFileLocator.DefaultFileName);
            await File.WriteAllTextAsync(configurationPath, "next-version: 2.0.0");

            var result = await new ProgramFixture().Run(directory.FullName, "config", "migrate", "--in-place");

            result.ExitCode.ShouldBe(0);
            var migratedConfiguration = await File.ReadAllTextAsync(configurationPath);
            migratedConfiguration.ShouldContain("calculation:");
            migratedConfiguration.ShouldContain("next-version: 2.0.0");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

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

    [Test]
    public void DefaultCalculationDisplaysConfigurationVersionMismatchOnStandardError()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        var configurationPath = FileSystemHelper.Path.Combine(fixture.RepositoryPath, ConfigurationFileLocator.DefaultFileName);
        FileSystemHelper.File.WriteAllText(configurationPath, "next-version: 2.0.0");

        var result = Execute(fixture.RepositoryPath, "v7");

        result.ExitCode.ShouldBe(1);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain("An error occurred:");
        result.Output.ShouldContain("uses the legacy v6 configuration structure");
        result.Output.ShouldContain("gitversion config migrate");
    }

    [Test]
    public void InvalidConfigurationVersionDisplaysDiagnosticOnStandardError()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        var result = Execute(fixture.RepositoryPath, "invalid");

        result.ExitCode.ShouldBe(1);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain("An error occurred:");
        result.Output.ShouldContain("Unrecognized GITVERSION_CONFIGURATION_VERSION value 'invalid'");
        result.Output.ShouldContain("Valid values are 'v6' and 'v7'");
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
