using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationValidationIntegrationTests
{
    private DirectoryInfo directory = null!;

    [SetUp]
    public void SetUp() => this.directory = Directory.CreateTempSubdirectory("gitversion-validate-");

    [TearDown]
    public void TearDown() => this.directory.Delete(true);

    [TestCase("{}")]
    [TestCase("")]
    [TestCase("calculation:\n  tag-prefix: '[vV]?'\noutput:\n  update-build-number: false")]
    [TestCase("workflow: GitFlow/v1")]
    [TestCase("workflow: GitHubFlow/v1")]
    [TestCase("workflow: TrunkBased/preview1")]
    public void ValidDocumentsHaveEmptyStdoutAndNoSideEffects(string text)
    {
        var file = Write(text);
        var before = Snapshot(this.directory.FullName);

        var result = Execute("config validate --config candidate.yml");

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("Configuration is valid.");
        File.ReadAllText(file).ShouldBe(text);
        Snapshot(this.directory.FullName).ShouldBe(before);
    }

    [TestCase("calculation: [", "GVCONFIG_YAML")]
    [TestCase("calculation:\n  unknown-key: secret-value", "GVCONFIG_PROPERTY")]
    [TestCase("output:\n  tag-prefix: secret-value", "GVCONFIG_LAYOUT")]
    [TestCase("tag-prefix: secret-value", "GVCONFIG_LAYOUT")]
    [TestCase("calculation:\n  increment: secret-value", "GVCONFIG_PROPERTY")]
    [TestCase("output:\n  update-build-number: secret-value", "GVCONFIG_PROPERTY")]
    [TestCase("output:\n  tag-pre-release-weight: secret-value", "GVCONFIG_PROPERTY")]
    [TestCase("calculation:\n  tag-prefix: '['", "GVCONFIG_REGEX")]
    [TestCase("calculation:\n  next-version: secret-value", "GVCONFIG_VALUE")]
    [TestCase("workflow: secret-value", "GVCONFIG_WORKFLOW")]
    [TestCase("workflow: ''", "GVCONFIG_WORKFLOW")]
    [TestCase("calculation:\n  branches:\n    main:\n      source-branches: [missing]", "GVCONFIG_BRANCH")]
    [TestCase("calculation:\n  branches:\n    main:\n      is-source-branch-for: [missing]", "GVCONFIG_BRANCH")]
    [TestCase("calculation:\n  branches:\n    custom:\n      label: custom", "GVCONFIG_BRANCH")]
    public void InvalidDocumentsProduceOneSafeJsonEnvelope(string text, string code)
    {
        Write(text);
        var before = Snapshot(this.directory.FullName);

        var result = Execute("config validate --config candidate.yml --format json");

        result.ExitCode.ShouldBe(1, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        var envelope = json.RootElement;
        envelope.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        envelope.GetProperty("valid").GetBoolean().ShouldBeFalse();
        envelope.GetProperty("configurationVersion").GetString().ShouldBe("v7");
        envelope.GetProperty("file").GetString().ShouldBe(Path.Combine(this.directory.FullName, "candidate.yml"));
        var diagnostic = envelope.GetProperty("diagnostics")[0];
        diagnostic.GetProperty("code").GetString().ShouldBe(code);
        diagnostic.GetProperty("severity").GetString().ShouldBe("error");
        diagnostic.GetProperty("path").ValueKind.ShouldBe(JsonValueKind.String);
        diagnostic.GetProperty("message").GetString().ShouldNotBeNullOrEmpty();
        if (code == "GVCONFIG_YAML")
        {
            diagnostic.GetProperty("line").GetInt32().ShouldBeGreaterThan(0);
        }

        result.Output.ShouldNotBeNull();
        result.Output.ShouldNotContain("secret-value");
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("Configuration is invalid.");
        Snapshot(this.directory.FullName).ShouldBe(before);
    }

    [TestCase("--override-config calculation.tag-prefix=runtime-", true, null)]
    [TestCase("--override-config calculation.strategies=Mainline", true, null)]
    [TestCase("--override-config workflow=GitHubFlow/v1", true, null)]
    [TestCase("--override-config calculation.tag-prefix=[", false, "GVCONFIG_REGEX")]
    [TestCase("--override-config calculation.increment=bad", false, "GVCONFIG_PROPERTY")]
    [TestCase("--override-config unknown=secret-value", false, "GVCONFIG_OVERRIDE")]
    [TestCase("--override-config secret-value", false, "GVCONFIG_OVERRIDE")]
    public void OverridesUseRuntimeRulesAndSafeDiagnostics(string overrides, bool valid, string? diagnosticCode)
    {
        Write("{}");
        var result = Execute("config validate --config candidate.yml --format json " + overrides);

        result.ExitCode.ShouldBe(valid ? 0 : 1, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("valid").GetBoolean().ShouldBe(valid);
        if (diagnosticCode is not null)
        {
            json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString().ShouldBe(diagnosticCode);
        }
        result.Output.ShouldNotBeNull();
        result.Output.ShouldNotContain("secret-value");
    }

    [Test]
    public void ExplicitV6CompatibilityWarnsWithoutFailing()
    {
        Write("tag-prefix: '[vV]?'");
        var result = Execute("config validate --config candidate.yml --format json", version: "v6");

        result.ExitCode.ShouldBe(0, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("configurationVersion").GetString().ShouldBe("v6");
        json.RootElement.GetProperty("valid").GetBoolean().ShouldBeTrue();
        json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString().ShouldBe("GVCONFIG_V6_COMPATIBILITY");
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("removed in GitVersion 7.1");
    }

    [TestCase("config validate --config missing.yml --format json")]
    [TestCase("config validate --format json")]
    public void MissingFilesNeverValidateDefaults(string command)
    {
        var result = Execute(command);

        result.ExitCode.ShouldBe(1, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("valid").GetBoolean().ShouldBeFalse();
        json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString().ShouldBe("GVCONFIG_FILE");
        Directory.GetFileSystemEntries(this.directory.FullName).ShouldBeEmpty();
    }

    [Test]
    public void ExplicitPathSupportsSpacesAndUnicodeOutsideGit()
    {
        var file = Path.Combine(this.directory.FullName, "config ü space.yml");
        File.WriteAllText(file, "{}");

        var result = Execute($"config validate --config \"{file}\" --format json");

        result.ExitCode.ShouldBe(0, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("file").GetString().ShouldBe(file);
        json.RootElement.GetProperty("diagnostics").GetArrayLength().ShouldBe(0);
    }

    [Test]
    public void DiscoveryPreservesSupportedFilenamePriority()
    {
        File.WriteAllText(Path.Combine(this.directory.FullName, "GitVersion.yaml"), "bad: [");
        File.WriteAllText(Path.Combine(this.directory.FullName, ".GitVersion.yml"), "bad: [");
        File.WriteAllText(Path.Combine(this.directory.FullName, "GitVersion.yml"), "{}");

        var result = Execute("config validate --format json");

        result.ExitCode.ShouldBe(0, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("file").GetString().ShouldBe(Path.Combine(this.directory.FullName, "GitVersion.yml"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RepositoryDiscoveryPreservesRootAndAmbiguityRules(bool ambiguous)
    {
        using var repository = new EmptyRepositoryFixture();
        var child = Directory.CreateDirectory(Path.Combine(repository.RepositoryPath, "child"));
        File.WriteAllText(Path.Combine(repository.RepositoryPath, "GitVersion.yml"), "{}");
        if (ambiguous)
        {
            File.WriteAllText(Path.Combine(child.FullName, "GitVersion.yml"), "{}");
        }

        var before = Snapshot(repository.RepositoryPath);

        var result = Execute("config validate --format json", workingDirectory: child.FullName);

        result.ExitCode.ShouldBe(ambiguous ? 1 : 0, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("valid").GetBoolean().ShouldBe(!ambiguous);
        if (ambiguous)
        {
            result.StandardError.ShouldNotBeNull();
            result.StandardError.ShouldContain("Ambiguous configuration file selection");
        }

        Snapshot(repository.RepositoryPath).ShouldBe(before);
    }

    [TestCase("--url https://invalid.example/repo")]
    [TestCase("--branch main")]
    [TestCase("--output buildserver")]
    [TestCase("--output-file generated.json")]
    [TestCase("--update-assembly-info")]
    [TestCase("--update-project-files")]
    [TestCase("--update-wix-version-file")]
    [TestCase("--diagnose")]
    [TestCase("--no-fetch")]
    [TestCase("--no-cache")]
    [TestCase("--no-normalize")]
    [TestCase("--show-config")]
    public void IncompatibleRootOptionsAreReportedWithoutSideEffects(string option)
    {
        Write("{}");
        var before = Snapshot(this.directory.FullName);
        var result = Execute($"{option} config validate --config candidate.yml --format json");

        result.ExitCode.ShouldBe(1, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString().ShouldBe("GVCONFIG_INVOCATION");
        Snapshot(this.directory.FullName).ShouldBe(before);
    }

    [TestCase("config validate --unknown --format json")]
    [TestCase("config validate --config")]
    [TestCase("config validate extra")]
    [TestCase("config validate --update-wix-version-file --format json")]
    [TestCase("config validate --url https://invalid.example/repo --format json")]
    public void ParserFailuresUseExistingStderrContract(string command)
    {
        var result = Execute(command);
        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("Could not parse command line parameter");
    }

    [Test]
    public void ValidationHelpDescribesTheCommandAndLocalJsonOption()
    {
        var result = Execute("config validate --help");

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput.ShouldNotBeNull();
        result.StandardOutput.ShouldContain("--format");
        result.StandardOutput.ShouldContain("versioned JSON");
        result.StandardOutput.ShouldContain("--override-config");
        result.StandardError.ShouldBeEmpty();
    }

    [Test]
    public void ValidationIsUnavailableWithLegacyArgumentParser()
    {
        var result = GitVersionHelper.ExecuteIn(this.directory.FullName, " config validate", false,
            new("GITVERSION_ARGUMENT_PARSER_VERSION", "v6"), new("GITVERSION_CONFIGURATION_VERSION", "v7"),
            new("GITVERSION_USE_V6_ARGUMENT_PARSER", null));

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
    }

    [TestCase("managed", "{}", true)]
    [TestCase("libgit2", "{}", true)]
    [TestCase("managed", "calculation: [", false)]
    [TestCase("libgit2", "calculation: [", false)]
    public async Task ExplicitValidationNeverConstructsGitOrCalculationServices(string backend, string content, bool valid)
    {
        Write(content);
        var originalBackend = SysEnv.GetEnvironmentVariable("GITVERSION_GIT_BACKEND");
        var originalVersion = SysEnv.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);
        try
        {
            SysEnv.SetEnvironmentVariable("GITVERSION_GIT_BACKEND", backend);
            SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, "v7");
            var fixture = new ProgramFixture(this.directory.FullName);
            fixture.WithOverrides(services =>
            {
                services.AddSingleton<IGitRepository>(_ => throw new AssertionException("Git backend constructed"));
                services.AddSingleton<IGitRepositoryInfo>(_ => throw new AssertionException("Repository discovery constructed"));
                services.AddSingleton<IGitVersionCalculateTool>(_ => throw new AssertionException("Calculation constructed"));
                services.AddSingleton<IGitVersionOutputTool>(_ => throw new AssertionException("Output constructed"));
            });
            var locator = Substitute.For<IConfigurationFileLocator>();
            fixture.WithOverrides(services => services.AddSingleton(locator));
            var result = await fixture.Run("config", "validate", "--config", "candidate.yml", "--format", "json");
            result.ExitCode.ShouldBe(valid ? 0 : 1, result.Output);
            using var json = JsonDocument.Parse(result.Output!);
            json.RootElement.GetProperty("valid").GetBoolean().ShouldBe(valid);
            locator.ReceivedCalls().ShouldBeEmpty();
        }
        finally
        {
            SysEnv.SetEnvironmentVariable("GITVERSION_GIT_BACKEND", originalBackend);
            SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, originalVersion);
        }
    }

    [TestCase("{}", true)]
    [TestCase("calculation:\n  tag-prefix: '['", false)]
    public void ExplicitValidationPreservesHistoryAndDirtyFiles(string content, bool valid)
    {
        using var repository = new EmptyRepositoryFixture();
        repository.MakeATaggedCommit("1.2.3");
        File.WriteAllText(Path.Combine(repository.RepositoryPath, "dirty.txt"), "uncommitted content");
        File.WriteAllText(Path.Combine(repository.RepositoryPath, "candidate.yml"), content);
        var before = Snapshot(repository.RepositoryPath);

        var result = Execute("config validate --config candidate.yml", workingDirectory: repository.RepositoryPath);

        result.ExitCode.ShouldBe(valid ? 0 : 1, result.Output);
        result.StandardOutput.ShouldBeEmpty();
        Snapshot(repository.RepositoryPath).ShouldBe(before);
    }

    [Test]
    public void NestedV7DocumentDoesNotFallBackWhenV6IsSelected()
    {
        Write("calculation:\n  tag-prefix: v");
        var result = Execute("config validate --config candidate.yml --format json", version: "v6");

        result.ExitCode.ShouldBe(1, result.Output);
        using var json = JsonDocument.Parse(result.StandardOutput!);
        json.RootElement.GetProperty("configurationVersion").GetString().ShouldBe("v6");
        json.RootElement.GetProperty("valid").GetBoolean().ShouldBeFalse();
        json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString().ShouldBe("GVCONFIG_LAYOUT");
    }

    [Test]
    public void InvalidLocalFormatIsAnExecutedInvocationError()
    {
        Write("{}");
        var result = Execute("config validate --config candidate.yml --format xml");

        result.ExitCode.ShouldBe(1, result.Output);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("GVCONFIG_INVOCATION");
    }

    private string Write(string text)
    {
        var path = Path.Combine(this.directory.FullName, "candidate.yml");
        File.WriteAllText(path, text);
        return path;
    }

    private static string[] Snapshot(string root) => [.. Directory.GetFiles(root, "*", SearchOption.AllDirectories)
        .Order(StringComparer.Ordinal).Select(path => path + ":" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))))];

    private ExecutionResults Execute(string command, string version = "v7", string? workingDirectory = null) =>
        GitVersionHelper.ExecuteIn(workingDirectory ?? this.directory.FullName, " " + command, false,
            new("GITVERSION_CONFIGURATION_VERSION", version), new("GITVERSION_ARGUMENT_PARSER_VERSION", "v7"),
            new("GITVERSION_GIT_BACKEND", "managed"), new("GITVERSION_USE_V6_ARGUMENT_PARSER", null));
}
