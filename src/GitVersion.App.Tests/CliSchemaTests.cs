using System.IO.Abstractions;
using System.Text.Json;
using GitVersion.App.Tests.Helpers;
using GitVersion.FileSystemGlobbing;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Serilog.Core;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class CliSchemaTests
{
    [TestCase("--cli-schema")]
    [TestCase("--cli-schema --config")]
    [TestCase("config --cli-schema")]
    [TestCase("config migrate --cli-schema")]
    [TestCase("--help --cli-schema")]
    [TestCase("--cli-schema --help")]
    [TestCase("--version --cli-schema")]
    [TestCase("--cli-schema --version")]
    [TestCase("--output invalid --cli-schema --version")]
    [TestCase("config migrate --help --cli-schema")]
    [TestCase("config migrate --cli-schema --help")]
    [TestCase("--config missing.yml --show-variable invalid --verbosity invalid --cli-schema")]
    [TestCase("--update-assembly-info *.cs --update-project-files *.csproj --cli-schema")]
    [TestCase("config migrate --config missing.yml --output result.yml --in-place --force --cli-schema")]
    public void ExportsBeforeMappingOrAuthentication(string arguments)
    {
        var environment = Substitute.For<IEnvironment>();
        var fileSystem = Substitute.For<IFileSystem>();
        var console = Substitute.For<IConsole>();
        var globbing = new UnexpectedGlobbingResolver();
        var parser = new ArgumentParser(environment, fileSystem, console, globbing, new LoggingLevelSwitch(), new CliSchemaExporter());

        var result = parser.ParseArguments(arguments);

        result.IsCliSchema.ShouldBeTrue();
        result.IsHelp.ShouldBeFalse();
        result.IsVersion.ShouldBeFalse();
        result.IsConfigurationMigration.ShouldBeFalse();
        environment.ReceivedCalls().ShouldBeEmpty();
        fileSystem.ReceivedCalls().ShouldBeEmpty();
        console.Received(1).WriteLine(Arg.Is<string>(value => value.StartsWith('{') && value.EndsWith('}')));
        console.ReceivedCalls().Count().ShouldBe(1);
    }

    [TestCase("--cli-schema --unknown")]
    [TestCase("--cli-schema --version --unknown")]
    [TestCase("--unknown --cli-schema")]
    [TestCase("--cli-schema first second")]
    [TestCase("config migrate unexpected --cli-schema")]
    [TestCase("-- --cli-schema")]
    [TestCase("--config --cli-schema --unknown")]
    public void PreservesUnmatchedAndPositionalDiagnostics(string arguments)
    {
        var console = Substitute.For<IConsole>();
        var parser = new ArgumentParser(Substitute.For<IEnvironment>(), Substitute.For<IFileSystem>(), console,
            new UnexpectedGlobbingResolver(), new LoggingLevelSwitch(), new CliSchemaExporter());

        Should.Throw<WarningException>(() => parser.ParseArguments(arguments));

        console.ReceivedCalls().ShouldBeEmpty();
    }

    [Test]
    public void TokenConsumedAsStringValueDoesNotRequestSchema()
    {
        var parser = new ArgumentParser(Substitute.For<IEnvironment>(), Substitute.For<IFileSystem>(), Substitute.For<IConsole>(),
            new UnexpectedGlobbingResolver(), new LoggingLevelSwitch(), new CliSchemaExporter());

        var result = parser.ParseArguments("config migrate --output --cli-schema");

        result.IsCliSchema.ShouldBeFalse();
        result.IsConfigurationMigration.ShouldBeTrue();
        result.MigrationOutputFile.ShouldBe("--cli-schema");
    }

    [Test]
    public void ReportsExpectedOutputFailure()
    {
        var console = Substitute.For<IConsole>();
        console.When(item => item.WriteLine(Arg.Any<string>())).Do(_ => throw new IOException("output closed"));
        var parser = new ArgumentParser(Substitute.For<IEnvironment>(), Substitute.For<IFileSystem>(), console,
            new UnexpectedGlobbingResolver(), new LoggingLevelSwitch(), new CliSchemaExporter());

        Should.Throw<WarningException>(() => parser.ParseArguments("--cli-schema"))
            .Message.ShouldBe("Could not export the CLI schema: output closed");
    }

    [Test]
    public async Task HostSkipsExecutorsAndFeatureLogging()
    {
        var fixture = new ProgramFixture();
        var calculation = new UnexpectedVersionExecutor();
        var migration = new UnexpectedMigrationExecutor();
        IHostApplicationLifetime? lifetime = null;
        fixture.WithOverrides(services =>
        {
            services.AddSingleton<IGitVersionExecutor>(sp =>
            {
                lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
                return calculation;
            });
            services.AddSingleton<IConfigurationMigrationExecutor>(migration);
        });

        var result = await fixture.Run("--cli-schema");

        result.ExitCode.ShouldBe(0);
        var applicationLifetime = lifetime ?? throw new AssertionException("The CLI did not resolve the application lifetime.");
        applicationLifetime.ApplicationStopping.IsCancellationRequested.ShouldBeTrue();
        result.Log.ShouldBeNullOrEmpty();
        CliSchemaExporterTests.ValidateSchema(result.Output!);
    }

    [TestCase("v6", "libgit2")]
    [TestCase("v7", "libgit2")]
    [TestCase("v6", "managed")]
    [TestCase("v7", "managed")]
    public void ProcessExportsSameDocumentWithoutRepositoryOrFiles(string configuration, string backend)
    {
        var directory = Directory.CreateTempSubdirectory("gitversion-schema-");
        try
        {
            var root = RunProcess(directory.FullName, "--cli-schema", "v7", configuration, backend);
            var child = RunProcess(directory.FullName, "config --cli-schema", "v7", configuration, backend);
            var migrate = RunProcess(directory.FullName,
                "config migrate --config missing.yml --in-place --output sentinel.yml --force --cli-schema", "v7", configuration, backend);
            var output = RunProcess(directory.FullName,
                "--config missing.yml --output file --output-file sentinel.json --log-file sentinel.log --cli-schema", "v7", configuration, backend);

            foreach (var result in new[] { root, child, migrate, output })
            {
                result.ExitCode.ShouldBe(0, result.StandardError);
                result.StandardError.ShouldBeEmpty();
                result.StandardOutput.ShouldBe(root.StandardOutput);
                result.StandardOutput.ShouldEndWith(System.Environment.NewLine);
            }
            Directory.GetFileSystemEntries(directory.FullName).ShouldBeEmpty();
            CliSchemaExporterTests.ValidateSchema(root.StandardOutput!);
            using var document = JsonDocument.Parse(root.StandardOutput!);
            var command = document.RootElement.GetProperty("command");
            document.RootElement.GetProperty("opencli").GetString().ShouldBe("0.1");
            var schema = CliSchemaExporterTests.FindOption(command, "--cli-schema");
            schema.GetProperty("recursive").GetBoolean().ShouldBeTrue();
            schema.GetProperty("arguments").GetArrayLength().ShouldBe(0);
            var formats = CliSchemaExporterTests.FindOption(command, "--output").GetProperty("arguments")[0];
            formats.GetProperty("acceptedValues").EnumerateArray().Select(value => value.GetString()).ShouldBe(Enum.GetNames<OutputType>());
            formats.GetProperty("arity").TryGetProperty("maximum", out _).ShouldBeFalse();
            var migrationCommand = command.GetProperty("commands")[0].GetProperty("commands")[0];
            migrationCommand.GetProperty("name").GetString().ShouldBe("migrate");
            CliSchemaExporterTests.FindOption(migrationCommand, "--output").GetProperty("arguments")[0]
                .TryGetProperty("acceptedValues", out _).ShouldBeFalse();
            CliSchemaExporterTests.FindOption(command, "--show-variable").GetProperty("arguments")[0]
                .GetProperty("acceptedValues").EnumerateArray().Select(value => value.GetString())
                .ShouldBe(GitVersionVariables.AvailableVariables);
            CliSchemaExporterTests.FindOption(command, "--verbosity").GetProperty("arguments")[0]
                .GetProperty("acceptedValues").EnumerateArray().Select(value => value.GetString())
                .ShouldBe(Enum.GetNames<Verbosity>());
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [TestCase("--cli-schema --unknown", "v7")]
    [TestCase("--cli-schema", "v6")]
    public void ProcessRejectsInvalidSyntaxAndLegacyParser(string arguments, string parser)
    {
        var directory = Directory.CreateTempSubdirectory("gitversion-schema-");
        try
        {
            var result = RunProcess(directory.FullName, arguments, parser, "v7", "managed");

            result.ExitCode.ShouldNotBe(0);
            result.StandardOutput.ShouldBeEmpty();
            result.StandardError!.ShouldContain("Could not parse command line parameter");
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [TestCase("--help --cli-schema")]
    [TestCase("--cli-schema --help")]
    [TestCase("--version --cli-schema")]
    [TestCase("--cli-schema --version")]
    [TestCase("config migrate --help --cli-schema")]
    [TestCase("config migrate --cli-schema --help")]
    public void ProcessEmitsOnlySchemaWhenCombinedWithHelpOrVersion(string arguments)
    {
        var directory = Directory.CreateTempSubdirectory("gitversion-schema-");
        try
        {
            var expected = RunProcess(directory.FullName, "--cli-schema", "v7", "v7", "managed");
            var actual = RunProcess(directory.FullName, arguments, "v7", "v7", "managed");

            expected.ExitCode.ShouldBe(0, expected.StandardError);
            actual.ExitCode.ShouldBe(0, actual.StandardError);
            actual.StandardOutput.ShouldBe(expected.StandardOutput);
            actual.StandardError.ShouldBeEmpty();
            Directory.GetFileSystemEntries(directory.FullName).ShouldBeEmpty();
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Test]
    public void ProcessIgnoresBuildServerOutputAndPreservesExistingConfiguration()
    {
        var directory = Directory.CreateTempSubdirectory("gitversion-schema-");
        var configurationPath = Path.Combine(directory.FullName, "GitVersion.yml");
        const string configurationContents = "mode: ContinuousDelivery\n";
        try
        {
            File.WriteAllText(configurationPath, configurationContents);
            KeyValuePair<string, string?>[] environment =
            [
                new("GITHUB_ACTIONS", "true"),
                new("GITHUB_OUTPUT", Path.Combine(directory.FullName, "github-output")),
                new("GITHUB_ENV", Path.Combine(directory.FullName, "github-env"))
            ];
            var expected = RunProcess(directory.FullName, "--cli-schema", "v7", "v7", "managed");
            var buildServer = RunProcess(directory.FullName,
                "--config GitVersion.yml --output buildserver --log-file console --cli-schema", "v7", "v7", "managed", environment);
            var migration = RunProcess(directory.FullName,
                "config migrate --config GitVersion.yml --in-place --cli-schema", "v7", "v7", "managed", environment);

            expected.ExitCode.ShouldBe(0, expected.StandardError);
            foreach (var result in new[] { buildServer, migration })
            {
                result.ExitCode.ShouldBe(0, result.StandardError);
                result.StandardOutput.ShouldBe(expected.StandardOutput);
                result.StandardError.ShouldBeEmpty();
            }
            File.ReadAllText(configurationPath).ShouldBe(configurationContents);
            Directory.GetFileSystemEntries(directory.FullName).ShouldBe([configurationPath]);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static ExecutionResults RunProcess(string directory, string arguments, string parser, string configuration, string backend)
        => RunProcess(directory, arguments, parser, configuration, backend, []);

    private static ExecutionResults RunProcess(string directory, string arguments, string parser, string configuration, string backend,
        KeyValuePair<string, string?>[] additionalEnvironment)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var exitCode = ProcessHelper.Run(line => stdout.AppendLine(line), line => stderr.AppendLine(line), null,
            ExecutableHelper.DotNetExecutable, ExecutableHelper.GetExecutableArgs(arguments), directory,
            [new("GITVERSION_ARGUMENT_PARSER_VERSION", parser), new("GITVERSION_CONFIGURATION_VERSION", configuration),
                new("GITVERSION_GIT_BACKEND", backend), .. additionalEnvironment]);
        return new(exitCode, stdout.ToString()) { StandardOutput = stdout.ToString(), StandardError = stderr.ToString() };
    }

    private sealed class UnexpectedGlobbingResolver : IGlobbingResolver
    {
        IEnumerable<string> IGlobbingResolver.Resolve(string workingDirectory, string pattern) =>
            throw new InvalidOperationException("Schema export must not resolve files.");
    }

    private sealed class UnexpectedMigrationExecutor : IConfigurationMigrationExecutor
    {
        int IConfigurationMigrationExecutor.Execute(GitVersionOptions options) =>
            throw new InvalidOperationException("Schema export must not execute migration.");
    }

    private sealed class UnexpectedVersionExecutor : IGitVersionExecutor
    {
        int IGitVersionExecutor.Execute(GitVersionOptions options) =>
            throw new InvalidOperationException("Schema export must not calculate a version.");
    }
}
