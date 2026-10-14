using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Logging.Abstractions;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class GitVersionExecutorRoutingTests
{
    private IConsole console = null!;
    private IConfigurationFileLocator locator = null!;
    private IConfigurationProvider provider = null!;
    private RecordingSerializer serializer = null!;
    private IGitRepository repository = null!;
    private IGitVersionCalculateTool calculator = null!;
    private IGitVersionOutputTool output = null!;
    private IGitVersionConfiguration configuration = null!;
    private Lazy<IGitVersionConfiguration> resolvedConfiguration = null!;
    private GitVersionExecutor executor = null!;
    private string? originalVersion;

    [SetUp]
    public void Setup()
    {
        this.originalVersion = SysEnv.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);
        SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, "v7");
        this.console = Substitute.For<IConsole>();
        this.locator = Substitute.For<IConfigurationFileLocator>();
        this.provider = Substitute.For<IConfigurationProvider>();
        this.serializer = new();
        this.repository = Substitute.For<IGitRepository>();
        var repositoryInfo = Substitute.For<IGitRepositoryInfo>();
        repositoryInfo.GitRootPath.Returns("repository");
        repositoryInfo.ProjectRootDirectory.Returns("project");
        repositoryInfo.DotGitDirectory.Returns(Guid.NewGuid().ToString("N"));
        this.calculator = Substitute.For<IGitVersionCalculateTool>();
        this.output = Substitute.For<IGitVersionOutputTool>();
        this.configuration = Substitute.For<IGitVersionConfiguration>();
        this.provider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(this.configuration);
        this.resolvedConfiguration = new(() => this.configuration);
        var logger = NullLogger<GitVersionExecutor>.Instance;
        var queryExecutor = new ConfigurationQueryExecutor(this.console, this.locator, this.provider, this.serializer, repositoryInfo);
        var runner = new GitVersionToolRunner(logger, this.resolvedConfiguration, this.calculator, this.output, this.repository, repositoryInfo);
        this.executor = new(logger, Substitute.For<IFileSystem>(), queryExecutor, runner);
    }

    [TearDown]
    public void RestoreVersion()
    {
        this.repository?.Dispose();
        SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, this.originalVersion);
    }

    [TestCase("v6")]
    [TestCase("v7")]
    public void DisplayUsesVersionSpecificOverridesWithoutCalculating(string version)
    {
        SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, version);
        var overrides = new Dictionary<object, object?> { ["tag-prefix"] = "custom-" };
        var options = new GitVersionOptions { WorkingDirectory = "working", ConfigurationInfo = { ShowConfiguration = true, OverrideConfiguration = overrides } };

        this.executor.Execute(options).ShouldBe(0);

        this.locator.Received(1).Verify("working", "project");
        this.provider.Received(1).Provide(version == "v7" ? overrides : null);
        this.console.Received(1).WriteLine("serialized configuration");
        this.serializer.Serialized.ShouldBeSameAs(this.configuration);
        this.serializer.PropertyPath.ShouldBeNull();
        AssertCalculationSkipped();
    }

    [Test]
    public void PropertyQueryUsesScalarSerializerAndSkipsDynamicRepositoryVerification()
    {
        var options = new GitVersionOptions { RepositoryInfo = { TargetUrl = "https://example.invalid/repository" }, ConfigurationInfo = { PropertyPath = "output.update-build-number" } };

        this.executor.Execute(options).ShouldBe(0);

        this.console.Received(1).WriteLine("true");
        this.serializer.Serialized.ShouldBeNull();
        this.serializer.PropertyConfiguration.ShouldBeSameAs(this.configuration);
        this.serializer.PropertyPath.ShouldBe("output.update-build-number");
        this.locator.ReceivedCalls().ShouldBeEmpty();
        AssertCalculationSkipped();
    }

    [Test]
    public void LegacyPropertyQueryStillThrowsBeforeLoadingConfiguration()
    {
        SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, "v6");
        var options = new GitVersionOptions { ConfigurationInfo = { PropertyPath = "output.update-build-number" } };

        Should.Throw<ConfigurationException>(() => this.executor.Execute(options))
            .Message.ShouldBe("Configuration property queries require v7 configuration. Set GITVERSION_CONFIGURATION_VERSION=v7.");

        this.provider.ReceivedCalls().ShouldBeEmpty();
        this.console.ReceivedCalls().ShouldBeEmpty();
        AssertCalculationSkipped();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NormalExecutionDiscoversCalculatesAndWritesOutputsInOrder(bool updateBuildNumber)
    {
        var variables = new GitVersionVariables(
            AssemblySemFileVer: "1.2.3.0", AssemblySemVer: "1.2.3.0", BranchName: "main",
            BuildMetaData: null, CommitDate: null, CustomVersion: null, EscapedBranchName: "main",
            FullBuildMetaData: null, FullSemVer: "1.2.3", InformationalVersion: "1.2.3",
            Major: "1", MajorMinorPatch: "1.2.3", Minor: "2", Patch: "3",
            PreReleaseLabelName: null, PreReleaseLabelNameWithDash: null, PreReleaseNumber: null,
            PreReleaseLabel: null, PreReleaseLabelWithDash: null, SemVer: "1.2.3", Sha: null,
            ShortSha: null, UncommittedChanges: "0", VersionSourceDistance: "0",
            VersionSourceIncrement: "None", VersionSourceSemVer: "1.2.3", VersionSourceSha: null,
            WeightedPreReleaseNumber: "0");
        this.calculator.CalculateVersionVariables().Returns(variables);
        this.configuration.UpdateBuildNumber.Returns(updateBuildNumber);

        this.executor.Execute(new GitVersionOptions()).ShouldBe(0);

        Received.InOrder(() =>
        {
            this.repository.DiscoverRepository("repository");
            this.calculator.CalculateVersionVariables();
            this.output.OutputVariables(variables, updateBuildNumber);
            this.output.UpdateAssemblyInfo(variables);
            this.output.UpdateWixVersionFile(variables);
        });
        this.resolvedConfiguration.IsValueCreated.ShouldBeTrue();
        this.provider.ReceivedCalls().ShouldBeEmpty();
        this.console.ReceivedCalls().ShouldBeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExpectedCalculationFailureReturnsOneAndWritesStderr(bool configurationError)
    {
        var exception = configurationError ? (Exception)new ConfigurationException("invalid configuration") : new WarningException("invalid repository");
        this.calculator.When(x => x.CalculateVersionVariables()).Do(_ => throw exception);
        var originalError = Console.Error;
        using var error = new StringWriter();
        Console.SetError(error);
        try
        {
            this.executor.Execute(new GitVersionOptions()).ShouldBe(1);

            error.ToString().ShouldBe($"An error occurred:{SysEnv.NewLine}{exception.Message}{SysEnv.NewLine}");
            this.output.ReceivedCalls().ShouldBeEmpty();
            this.resolvedConfiguration.IsValueCreated.ShouldBeFalse();
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    private void AssertCalculationSkipped()
    {
        this.repository.ReceivedCalls().ShouldBeEmpty();
        this.calculator.ReceivedCalls().ShouldBeEmpty();
        this.output.ReceivedCalls().ShouldBeEmpty();
        this.resolvedConfiguration.IsValueCreated.ShouldBeFalse();
    }

    private sealed class RecordingSerializer : IConfigurationSerializer
    {
        public object? Serialized { get; private set; }
        public IGitVersionConfiguration? PropertyConfiguration { get; private set; }
        public string? PropertyPath { get; private set; }

#pragma warning disable S2325, S1172 // Sonar does not collect NOSONAR metadata for test projects; suppress the interface-member false positives directly.
        public T Deserialize<T>(string input) => throw new NotSupportedException("These execution tests only serialize configuration."); // NOSONAR S2325, S1172: IConfigurationSerializer requires an instance method and this input parameter.
#pragma warning restore S2325, S1172

        public string Serialize(object graph)
        {
            Serialized = graph;
            return "serialized configuration";
        }

        public string SerializeProperty(IGitVersionConfiguration configuration, string path)
        {
            PropertyConfiguration = configuration;
            PropertyPath = path;
            return "true";
        }
    }
}
