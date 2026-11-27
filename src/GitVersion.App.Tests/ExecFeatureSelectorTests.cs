namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class ExecFeatureSelectorTests
{
    [Test]
    [Combinatorial]
    public void SelectorCombinationsCalculateAndLogWithoutPollutingOutput(
        [Values("v7")] string parser,
        [Values("v7")] string configuration,
        [Values("managed")] string backend,
        [Values("json", "variable", "config")] string output,
        [Values(false, true)] bool buildServer)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("release-1.2.3");
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "GitVersion.yml"),
            "calculation:\n  tag-prefix: release-");
        var logPath = Path.Combine(fixture.RepositoryPath, "selectors.log");
        var outputArgument = OutputArguments(output, buildServer);
        var arguments = $"\"{fixture.RepositoryPath}\" --log-file \"{logPath}\" {outputArgument}";

        var result = GitVersionHelper.ExecuteIn(null, arguments, false, Selectors(parser, configuration, backend));

        result.ExitCode.ShouldBe(0, result.Output);
        var stdout = result.StandardOutput!;
        stdout.ShouldNotContain("INFO [");
        if (buildServer && output != "config")
        {
            var integrationOutput = $"Set Build Number for 'LocalBuild'.{SysEnv.NewLine}{SysEnv.NewLine}Set Output Variables for 'LocalBuild'.{SysEnv.NewLine}";
            stdout.ShouldStartWith(integrationOutput);
            stdout = stdout[integrationOutput.Length..];
        }
        if (output == "json")
        {
            using var json = JsonDocument.Parse(stdout);
            json.RootElement.GetProperty("FullSemVer").GetString().ShouldBe("1.2.3");
        }
        else if (output == "variable")
        {
            stdout.Trim().ShouldBe("1.2.3");
        }
        else
        {
            stdout.ShouldContain("tag-prefix: release-");
            stdout.ShouldContain("calculation:");
        }

        stdout.ShouldNotContain("Argument parser version:");
        stdout.ShouldNotContain("Configuration version:");
        stdout.ShouldNotContain("Git backend:");
        if (buildServer)
        {
            result.StandardError.ShouldNotBeNull();
            result.StandardError.ShouldContain($"Argument parser version: {parser}");
        }
        var log = File.ReadAllText(logPath);
        log.ShouldContain($"Argument parser version: {parser}");
        log.ShouldContain($"Configuration version: {configuration}");
        log.ShouldContain($"Git backend: {backend}");
        log.Split("Git backend:").Length.ShouldBe(2);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t ")]
    public void DefaultsSelectV7AndManagedWithConsoleLogsOnStderr(string? value)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "GitVersion.yml"), "calculation:\n  tag-prefix: '[vV]?'");

        var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, " --show-variable FullSemVer --log-file console", false,
            Selectors(value, value, value));

        result.ExitCode.ShouldBe(0, result.Output);
        result.StandardOutput!.Trim().ShouldBe("1.2.3");
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain("Argument parser version: v7");
        result.StandardError.ShouldContain("Configuration version: v7");
        result.StandardError.ShouldContain("Git backend: managed");
    }

    [TestCase("GITVERSION_ARGUMENT_PARSER_VERSION", "v6", "POSIX-style arguments")]
    [TestCase("GITVERSION_CONFIGURATION_VERSION", " V6 ", "gitversion config migrate")]
    [TestCase("GITVERSION_ARGUMENT_PARSER_VERSION", "true", "'v7'")]
    [TestCase("GITVERSION_CONFIGURATION_VERSION", "v8", "'v7'")]
    [TestCase("GITVERSION_GIT_BACKEND", "manged", "'managed'")]
    [TestCase("GITVERSION_GIT_BACKEND", "libgit2", "removed in v7.1")]
    [TestCase("GITVERSION_GIT_BACKEND", " LiBgIt2 ", "removed in v7.1")]
    [TestCase("GITVERSION_USE_V6_ARGUMENT_PARSER", "true", "GITVERSION_ARGUMENT_PARSER_VERSION=v7")]
    [TestCase("GITVERSION_USE_V6_ARGUMENT_PARSER", "false", "GITVERSION_ARGUMENT_PARSER_VERSION=v7")]
    [TestCase("GITVERSION_USE_V6_ARGUMENT_PARSER", "", "GITVERSION_ARGUMENT_PARSER_VERSION=v7")]
    public void InvalidSelectorsFailBeforeHelpWithActionableStderr(string variable, string value, string guidance)
    {
        var environment = Selectors("v7", "v7", "managed").ToDictionary(pair => pair.Key, pair => pair.Value);
        environment[variable] = value;

        var result = GitVersionHelper.ExecuteIn(null, "--help", false, [.. environment]);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldNotBeNull();
        result.StandardError.ShouldContain(variable);
        result.StandardError.ShouldContain(guidance);
        result.StandardError.ShouldNotContain("Unhandled exception");
    }

    [Test]
    [Combinatorial]
    public void MigrationRetainsV6InputAndYamlStdoutWithEitherBackend(
        [Values("v7")] string configuration,
        [Values("managed")] string backend)
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var input = Path.Combine(directory.FullName, "GitVersion.yml");
            File.WriteAllText(input, "next-version: 2.0.0");
            var result = GitVersionHelper.ExecuteIn(null,
                $"--log-file console config migrate --config \"{input}\"", false, Selectors("v7", configuration, backend));

            result.ExitCode.ShouldBe(0, result.Output);
            result.StandardOutput.ShouldNotBeNull();
            result.StandardError.ShouldNotBeNull();
            result.StandardOutput.ShouldContain("calculation:");
            result.StandardOutput.ShouldContain("  next-version: 2.0.0");
            result.StandardOutput.ShouldNotContain("INFO");
            result.StandardError.ShouldContain($"Configuration version: {configuration}");
            result.StandardError.ShouldContain($"Git backend: {backend}");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string OutputArguments(string output, bool buildServer)
    {
        var arguments = output switch
        {
            "variable" => "--output json --show-variable FullSemVer",
            "config" => "--show-config",
            _ => "--output json"
        };
        return buildServer ? arguments + " --output buildserver" : arguments;
    }

    private static KeyValuePair<string, string?>[] Selectors(string? parser, string? configuration, string? backend) =>
    [
        new("GITVERSION_ARGUMENT_PARSER_VERSION", parser),
        new("GITVERSION_CONFIGURATION_VERSION", configuration),
        new("GITVERSION_GIT_BACKEND", backend),
        new("GITVERSION_USE_V6_ARGUMENT_PARSER", null)
    ];
}
