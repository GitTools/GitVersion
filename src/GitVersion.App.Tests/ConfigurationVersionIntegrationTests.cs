using GitVersion.App.Tests.Helpers;
using GitVersion.Configuration;
using GitVersion.Helpers;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationVersionIntegrationTests
{
    [TestCase("ShortSha")]
    [TestCase("Sha")]
    public async Task V6V7AndMigratedConfigurationCalculateCurrentCommitLabel(string placeholder)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("feature/topic");
        fixture.MakeACommit();
        var sha = fixture.Repository.Head.Tip.Sha;
        var label = $"ci.{{{placeholder}}}";
        var configurationPath = Path.Combine(fixture.RepositoryPath, ConfigurationFileLocator.DefaultFileName);
        await File.WriteAllTextAsync(configurationPath, $"next-version: 2.0.0\nbranches:\n  feature:\n    label: '{label}'");

        var v6 = Execute(fixture.RepositoryPath, "v6");
        var migration = await new ProgramFixture(fixture.RepositoryPath).Run("config", "migrate");

        migration.ExitCode.ShouldBe(0);
        migration.Output.ShouldNotBeNull();
        var migratedConfiguration = new ConfigurationSerializer().Deserialize<GitVersionConfiguration>(migration.Output);
        migratedConfiguration.Branches["feature"].Label.ShouldBe(label);
        await File.WriteAllTextAsync(configurationPath, migration.Output);
        var migrated = Execute(fixture.RepositoryPath, "v7");

        await File.WriteAllTextAsync(configurationPath, $"calculation:\n  next-version: 2.0.0\n  branches:\n    feature:\n      label: '{label}'\noutput: {{}}");
        var v7 = Execute(fixture.RepositoryPath, "v7");

        foreach (var result in new[] { v6, v7, migrated })
        {
            result.ExitCode.ShouldBe(0);
            using var json = JsonDocument.Parse(result.StandardOutput!);
            json.RootElement.GetProperty("PreReleaseLabelName").GetString().ShouldBe("ci." + (placeholder == "Sha" ? sha : sha[..7]));
            json.RootElement.GetProperty("Sha").GetString().ShouldBe(sha);
            json.RootElement.GetProperty("ShortSha").GetString().ShouldBe(sha[..7]);
            GetFullSemVer(result.StandardOutput!).ShouldBe(GetFullSemVer(v6.StandardOutput!));
        }
    }

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
            forceResult.StandardError.ShouldNotBeNull();
            forceResult.StandardError.Split("Comments cannot be preserved during migration.").Length.ShouldBe(2);
            forceResult.StandardOutput.ShouldBeEmpty();
            var forcedOutput = await File.ReadAllTextAsync(outputPath);
            forcedOutput.ShouldContain("calculation:");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ConfigMigrateDiscoversRootConfigurationUnlessWorkingDirectoryHasOne(bool hasLocalConfiguration)
    {
        using var repository = new EmptyRepositoryFixture();
        var rootConfiguration = Path.Combine(repository.RepositoryPath, ConfigurationFileLocator.DefaultFileName);
        await File.WriteAllTextAsync(rootConfiguration, "next-version: 2.0.0");
        var subdirectory = Directory.CreateDirectory(Path.Combine(repository.RepositoryPath, "subdirectory"));
        if (hasLocalConfiguration)
        {
            await File.WriteAllTextAsync(Path.Combine(subdirectory.FullName, ConfigurationFileLocator.DefaultFileName), "next-version: 3.0.0");
        }

        var result = await new ProgramFixture(subdirectory.FullName).Run("config", "migrate");

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain(hasLocalConfiguration ? "next-version: 3.0.0" : "next-version: 2.0.0");
        (await File.ReadAllTextAsync(rootConfiguration)).ShouldBe("next-version: 2.0.0");
    }

    [Test]
    public async Task ConfigMigrateLeavesNoTemporaryFileAfterWritingOutput()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var configurationPath = Path.Combine(directory.FullName, ConfigurationFileLocator.DefaultFileName);
            await File.WriteAllTextAsync(configurationPath, "next-version: 2.0.0");

            var result = await new ProgramFixture(directory.FullName).Run("config", "migrate", "--output", "GitVersion.v7.yml");

            result.ExitCode.ShouldBe(0);
            var files = Directory.GetFiles(directory.FullName).Select(Path.GetFileName).ToArray();
            files.Length.ShouldBe(2);
            files.ShouldContain(ConfigurationFileLocator.DefaultFileName);
            files.ShouldContain("GitVersion.v7.yml");
            files.ShouldNotContain(file => file!.StartsWith(".", StringComparison.Ordinal));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ConfigMigrateRejectsInvalidInputWithoutReplacingFile()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            const string input = "output:\n  increment: Major";
            var configurationPath = Path.Combine(directory.FullName, ConfigurationFileLocator.DefaultFileName);
            await File.WriteAllTextAsync(configurationPath, input);

            var result = await new ProgramFixture(directory.FullName).Run("config", "migrate", "--in-place");

            result.ExitCode.ShouldBe(1);
            result.Output.ShouldBeEmpty();
            (await File.ReadAllTextAsync(configurationPath)).ShouldBe(input);
            Directory.GetFiles(directory.FullName).ShouldBe([configurationPath]);
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
            result.StandardError.ShouldNotBeNull();
            result.StandardError.Split("Comments cannot be preserved during migration.").Length.ShouldBe(2);
            result.StandardOutput.ShouldBeEmpty();
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
    public async Task ConfigMigrateDoesNotEmitLegacyFallbackWarning()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var configurationPath = Path.Combine(directory.FullName, ConfigurationFileLocator.DefaultFileName);
            await File.WriteAllTextAsync(configurationPath, "next-version: 2.0.0");
            var fixture = new ProgramFixture(directory.FullName);
            fixture.WithEnv(new KeyValuePair<string, string>(ConfigurationVersionSelector.EnvironmentVariableName, "v6"));

            var result = await fixture.Run("config", "migrate");

            result.ExitCode.ShouldBe(0);
            result.Output!.ShouldNotContain("temporary v6 compatibility mode");
            result.Log!.ShouldNotContain("temporary v6 compatibility mode");
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
        GetFullSemVer(v7Result.StandardOutput!).ShouldBe(GetFullSemVer(v6Result.StandardOutput!));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExplicitV6WarningUsesStandardErrorOnceWithoutContaminatingJson(bool logToFile)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        var configurationPath = Path.Combine(fixture.RepositoryPath, ConfigurationFileLocator.DefaultFileName);
        File.WriteAllText(configurationPath, "next-version: 2.0.0");

        var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, " --no-cache", logToFile,
            new KeyValuePair<string, string?>(ConfigurationVersionSelector.EnvironmentVariableName, "v6"));

        result.ExitCode.ShouldBe(0);
        GetFullSemVer(result.StandardOutput!).ShouldBe("2.0.0-1");
        result.StandardError.ShouldNotBeNull();
        result.StandardError.Split("temporary v6 compatibility mode").Length.ShouldBe(2);
        result.StandardError.ShouldContain(configurationPath);
        result.StandardError.ShouldContain("GitVersion 7.1");
        result.StandardError.ShouldContain("gitversion config migrate");
        result.StandardError.ShouldContain("GITVERSION_CONFIGURATION_VERSION=v7");
        if (logToFile)
        {
            result.Log.ShouldNotBeNull();
            result.Log.ShouldContain("temporary v6 compatibility mode");
        }
    }

    [TestCase("v6", "")]
    [TestCase("v7", "")]
    [TestCase("v6", "workflow: GitHubFlow/v1")]
    [TestCase("v7", "workflow: GitHubFlow/v1")]
    public void CalculatesVersionWithSharedRootConfiguration(string version, string configuration)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, ConfigurationFileLocator.DefaultFileName), configuration);

        var result = Execute(fixture.RepositoryPath, version);

        result.ExitCode.ShouldBe(0);
        GetFullSemVer(result.StandardOutput!).ShouldNotBeNullOrEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RootWorkflowAppliesDefaultsToBothSectionsAndPreservesOverrides(bool hasOverrides)
    {
        using var fixture = new EmptyRepositoryFixture();
        var configuration = "workflow: GitHubFlow/v1";
        if (hasOverrides)
        {
            configuration += """

                             calculation:
                               tag-prefix: custom-
                               branches:
                                 main:
                                   increment: Major
                             output:
                               assembly-versioning-scheme: None
                               branches:
                                 main:
                                   pre-release-weight: 42
                             """;
        }
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, ConfigurationFileLocator.DefaultFileName), configuration);

        var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, " --show-config", logToFile: false,
            new KeyValuePair<string, string?>(ConfigurationVersionSelector.EnvironmentVariableName, "v7"));

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain("workflow: GitHubFlow/v1");
        result.Output.ShouldNotContain("  workflow:");
        var document = new ConfigurationSerializer().Deserialize<Dictionary<object, object?>>(result.Output);
        var effective = new ConfigurationHelper(ConfigurationDocumentMapper.Flatten(document)).Configuration;
        effective.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        effective.TagPrefixPattern.ShouldBe(hasOverrides ? "custom-" : "[vV]?");
        effective.AssemblyVersioningScheme.ShouldBe(hasOverrides ? AssemblyVersioningScheme.None : AssemblyVersioningScheme.MajorMinorPatch);
        effective.Branches["main"].Increment.ShouldBe(hasOverrides ? IncrementStrategy.Major : IncrementStrategy.Patch);
        effective.Branches["main"].PreReleaseWeight.ShouldBe(hasOverrides ? 42 : 55000);
    }

    [Test]
    public void ExplicitV6WithoutUserConfigurationDoesNotWarnOnStandardError()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        var result = Execute(fixture.RepositoryPath, "v6");

        result.ExitCode.ShouldBe(0);
        result.StandardError.ShouldBeEmpty();
        GetFullSemVer(result.StandardOutput!).ShouldNotBeNullOrEmpty();
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
