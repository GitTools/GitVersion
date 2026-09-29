using GitVersion.Configuration;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationQueryIntegrationTests
{
    [TestCase(null)]
    [TestCase("GitFlow/v1")]
    [TestCase("GitHubFlow/v1")]
    [TestCase("TrunkBased/preview1")]
    public void QueryUsesDefaultAndWorkflowConfiguration(string? workflow)
    {
        using var fixture = new EmptyRepositoryFixture();
        if (workflow is not null)
        {
            File.WriteAllText(Path.Combine(fixture.RepositoryPath, "GitVersion.yml"), $"workflow: {workflow}");
        }

        var result = Execute(fixture.RepositoryPath, "config get calculation.tag-prefix", version: null);

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput.ShouldBe("\"[vV]?\"" + SysEnv.NewLine);
        result.StandardError.ShouldBeEmpty();
        if (workflow is not null)
        {
            var workflowResult = Execute(fixture.RepositoryPath, "config get workflow");
            workflowResult.ExitCode.ShouldBe(0, workflowResult.Output);
            workflowResult.StandardOutput.ShouldBe(JsonSerializer.Serialize(workflow) + SysEnv.NewLine);
        }
    }

    [Test]
    [Combinatorial]
    public void QueryAndDisplayHonorFileThenRuntimeOverrides(
        [Values("managed", "libgit2")] string backend,
        [Values(false, true)] bool logToFile)
    {
        using var fixture = new EmptyRepositoryFixture();
        var configPath = Path.Combine(fixture.RepositoryPath, "GitVersion.yml");
        const string file = "workflow: GitFlow/v1\ncalculation:\n  tag-prefix: file-\noutput:\n  tag-pre-release-weight: 12\n";
        File.WriteAllText(configPath, file);
        const string overrides = " --override-config workflow=GitHubFlow/v1 --override-config calculation.tag-prefix=runtime- --override-config output.tag-pre-release-weight=0";

        var result = Execute(fixture.RepositoryPath, "config get calculation.tag-prefix" + overrides, backend: backend, logToFile: logToFile);
        var numeric = Execute(fixture.RepositoryPath, "config get output.tag-pre-release-weight" + overrides, backend: backend);
        var display = Execute(fixture.RepositoryPath, "--show-config" + overrides, backend: backend);

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput.ShouldBe("\"runtime-\"" + SysEnv.NewLine);
        numeric.ExitCode.ShouldBe(0, numeric.Output);
        numeric.StandardOutput.ShouldBe("0" + SysEnv.NewLine);
        display.ExitCode.ShouldBe(0, display.Output);
        display.StandardOutput.ShouldNotBeNull();
        display.StandardOutput.ShouldContain("tag-prefix: runtime-");
        display.StandardOutput.ShouldContain("workflow: GitHubFlow/v1");
        display.StandardOutput.ShouldContain("tag-pre-release-weight: 0");
        if (logToFile)
        {
            result.Log.ShouldNotBeNull();
            result.Log.ShouldContain("Configuration version: v7");
        }
        File.ReadAllText(configPath).ShouldBe(file);
    }

    [TestCase("output.assembly-versioning-format", "null")]
    [TestCase("output.update-build-number", "false")]
    [TestCase("output.custom-version-format", "\"null\"")]
    public void QueryPreservesNullBooleanAndStringValues(string path, string expected)
    {
        using var fixture = new EmptyRepositoryFixture();
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "GitVersion.yml"),
            "output:\n  assembly-versioning-format: null\n  update-build-number: false\n  custom-version-format: 'null'");

        var result = Execute(fixture.RepositoryPath, $"config get {path}");

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput.ShouldBe(expected + SysEnv.NewLine);
    }

    [TestCase("v6", "require v7 configuration")]
    [TestCase("invalid", "Unrecognized GITVERSION_CONFIGURATION_VERSION")]
    public void QueryRejectsUnsupportedConfigurationBeforeReadingFile(string version, string diagnostic)
    {
        using var fixture = new EmptyRepositoryFixture();
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "GitVersion.yml"), "invalid: [");

        var result = Execute(fixture.RepositoryPath, "config get calculation.tag-prefix", version);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain(diagnostic);
    }

    [TestCase("config get calculation.missing", "Unknown configuration property")]
    [TestCase("config get tag-prefix", "Unknown configuration property")]
    [TestCase("config get calculation..tag-prefix", "Invalid configuration property path")]
    [TestCase("config get output", "only scalar properties")]
    [TestCase("config get calculation.branches.main.increment", "maps and collections are not supported")]
    [TestCase("config get calculation.strategies", "maps and collections are not supported")]
    [TestCase("config get", "Could not parse command line parameter")]
    [TestCase("--update-wix-version-file config get calculation.tag-prefix", "cannot be used with 'config get'")]
    public void QueryErrorsUseOnlyStderr(string command, string diagnostic)
    {
        using var fixture = new EmptyRepositoryFixture();

        var result = Execute(fixture.RepositoryPath, command);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain(diagnostic);
        File.Exists(Path.Combine(fixture.RepositoryPath, "GitVersion_WixVersion.wxi")).ShouldBeFalse();
    }

    [Test]
    public void QueryRejectsLegacyDocumentUnderV7()
    {
        using var fixture = new EmptyRepositoryFixture();
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "GitVersion.yml"), "tag-prefix: legacy-");

        var result = Execute(fixture.RepositoryPath, "config get calculation.tag-prefix");

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("uses the legacy v6 configuration structure");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void QueryDiscoversLocalOrRootConfigurationAndSupportsExplicitFile(bool local)
    {
        using var fixture = new EmptyRepositoryFixture();
        var subdirectory = Directory.CreateDirectory(Path.Combine(fixture.RepositoryPath, "child"));
        File.WriteAllText(Path.Combine(local ? subdirectory.FullName : fixture.RepositoryPath, "GitVersion.yml"),
            "calculation:\n  tag-prefix: " + (local ? "local-" : "root-"));
        File.WriteAllText(Path.Combine(subdirectory.FullName, "custom.yml"), "calculation:\n  tag-prefix: explicit-");

        var discovered = Execute(subdirectory.FullName, "config get calculation.tag-prefix");
        var explicitFile = Execute(subdirectory.FullName, "config get calculation.tag-prefix --config custom.yml");

        discovered.ExitCode.ShouldBe(0, discovered.Output);
        discovered.StandardOutput.ShouldBe(JsonSerializer.Serialize(local ? "local-" : "root-") + SysEnv.NewLine);
        explicitFile.ExitCode.ShouldBe(0, explicitFile.Output);
        explicitFile.StandardOutput.ShouldBe("\"explicit-\"" + SysEnv.NewLine);
    }

    [Test]
    public void QueryWorksWithoutGitHistoryAndKeepsVerboseLogsOffStdout()
    {
        using var fixture = new EmptyRepositoryFixture();
        var before = Directory.GetFileSystemEntries(fixture.RepositoryPath);

        var result = Execute(fixture.RepositoryPath, "config get output.update-build-number --verbosity Diagnostic --log-file console");

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput.ShouldBe("true" + SysEnv.NewLine);
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("Configuration version: v7");
        Directory.GetFileSystemEntries(fixture.RepositoryPath).ShouldBe(before);
    }

    [Test]
    public void QueryPreservesAmbiguousConfigurationDiagnostic()
    {
        using var fixture = new EmptyRepositoryFixture();
        var child = Directory.CreateDirectory(Path.Combine(fixture.RepositoryPath, "child"));
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "GitVersion.yml"), "calculation:\n  tag-prefix: root-");
        File.WriteAllText(Path.Combine(child.FullName, "GitVersion.yml"), "calculation:\n  tag-prefix: local-");

        var result = Execute(child.FullName, "config get calculation.tag-prefix");

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("Ambiguous configuration file selection");
    }

    [Test]
    public async Task QueryNeverCallsVersionCalculationOrVersionOutput()
    {
        using var repository = new EmptyRepositoryFixture();
        var calculate = Substitute.For<IGitVersionCalculateTool>();
        var output = Substitute.For<IGitVersionOutputTool>();
        var fixture = new ProgramFixture(repository.RepositoryPath);
        fixture.WithOverrides(services =>
        {
            services.AddSingleton(calculate);
            services.AddSingleton(output);
        });
        var original = SysEnv.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);
        try
        {
            SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, "v7");

            var result = await fixture.Run("config", "get", "output.update-build-number");

            result.ExitCode.ShouldBe(0, result.Output);
            result.Output.ShouldBe("true" + SysEnv.NewLine);
            calculate.ReceivedCalls().ShouldBeEmpty();
            output.ReceivedCalls().ShouldBeEmpty();
        }
        finally
        {
            SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, original);
        }
    }

    [Test]
    public void QueryIsUnavailableWithLegacyArgumentParser()
    {
        using var fixture = new EmptyRepositoryFixture();

        var result = GitVersionHelper.ExecuteIn(null, $"\"{fixture.RepositoryPath}\" config get calculation.tag-prefix", false,
            new("GITVERSION_CONFIGURATION_VERSION", "v7"),
            new("GITVERSION_ARGUMENT_PARSER_VERSION", "v6"),
            new("GITVERSION_USE_V6_ARGUMENT_PARSER", null));

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
    }

    [TestCase("v6", "tag-prefix", "[vV]?")]
    [TestCase("v7", "calculation.tag-prefix", "overridden-")]
    public void ShowConfigOverrideCorrectionIsLimitedToV7(string version, string path, string expected)
    {
        using var fixture = new EmptyRepositoryFixture();

        var result = Execute(fixture.RepositoryPath, $"--show-config --override-config {path}=overridden-", version);

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput.ShouldNotBeNull();
        var document = new ConfigurationSerializer().Deserialize<Dictionary<object, object?>>(result.StandardOutput);
        var calculation = version == "v7" ? (Dictionary<object, object?>)document["calculation"]! : document;
        calculation["tag-prefix"].ShouldBe(expected);
    }

    private static ExecutionResults Execute(string directory, string command, string? version = "v7", string backend = "managed", bool logToFile = false) =>
        GitVersionHelper.ExecuteIn(directory, " " + command, logToFile,
            new("GITVERSION_CONFIGURATION_VERSION", version),
            new("GITVERSION_ARGUMENT_PARSER_VERSION", "v7"),
            new("GITVERSION_GIT_BACKEND", backend),
            new("GITVERSION_USE_V6_ARGUMENT_PARSER", null));
}
