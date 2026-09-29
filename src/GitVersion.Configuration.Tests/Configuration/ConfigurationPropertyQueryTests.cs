namespace GitVersion.Configuration.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationPropertyQueryTests
{
    private readonly ConfigurationSerializer serializer = new();
    private string? originalVersion;

    [SetUp]
    public void SetUp()
    {
        this.originalVersion = SysEnv.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);
        SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, "v7");
    }

    [TearDown]
    public void TearDown() => SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, this.originalVersion);

    [TestCase("")]
    [TestCase("null")]
    [TestCase("true")]
    [TestCase("123")]
    [TestCase("2026-09-29")]
    [TestCase("[vV]?")]
    [TestCase("  quoted \"value\" \\ café\nsecond line\t")]
    [TestCase("{Major}.{env:BUILD_NUMBER ?? 0}")]
    public void PreservesStringTypeAndContents(string value)
    {
        var configuration = new GitVersionConfiguration { CustomVersionFormat = value };

        var result = this.serializer.SerializeProperty(configuration, "output.custom-version-format");

        using var json = JsonDocument.Parse(result);
        json.RootElement.ValueKind.ShouldBe(JsonValueKind.String);
        json.RootElement.GetString().ShouldBe(value);
        result.ShouldNotContain('\n');
    }

    [TestCase("output.update-build-number", "false")]
    [TestCase("output.pre-release-weight", "0")]
    [TestCase("output.tag-pre-release-weight", "60000")]
    [TestCase("output.assembly-versioning-scheme", "\"MajorMinor\"")]
    [TestCase("calculation.prevent-increment.of-merged-branch", "true")]
    [TestCase("calculation.ignore.commits-before", "\"2026-01-02T03:04:05Z\"")]
    [SetCulture("fr-FR")]
    public void UsesPublicScalarRepresentationAndInvariantNumbers(string path, string expected)
    {
        var configuration = new GitVersionConfiguration
        {
            UpdateBuildNumber = false,
            PreReleaseWeight = 0,
            TagPreReleaseWeight = 60000,
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor,
            PreventIncrement = new() { OfMergedBranch = true },
            Ignore = new() { BeforeString = "2026-01-02T03:04:05Z" }
        };

        this.serializer.SerializeProperty(configuration, path).ShouldBe(expected);
    }

    [TestCase("workflow")]
    [TestCase("calculation.next-version")]
    [TestCase("calculation.prevent-increment.of-merged-branch")]
    [TestCase("calculation.ignore.commits-before")]
    public void KnownNullIsQueryableEvenWhenOmittedFromFullDisplay(string path)
    {
        var configuration = new GitVersionConfiguration();

        this.serializer.SerializeProperty(configuration, path).ShouldBe("null");
        this.serializer.Serialize(configuration).ShouldNotContain(path.Split('.')[^1] + ":");
        Should.Throw<ConfigurationException>(() => this.serializer.SerializeProperty(configuration, path + "-unknown"))
            .Message.ShouldContain("Unknown configuration property");
    }

    [TestCase("tag-prefix")]
    [TestCase("calculation.tagprefixpattern")]
    [TestCase("calculation.update-build-number")]
    [TestCase("output.tag-prefix")]
    [TestCase("output.workflow")]
    [TestCase("workflow.child")]
    [TestCase("calculation.tag-prefix.child")]
    [TestCase("calculation.missing")]
    public void RejectsUnknownPublicPaths(string path) =>
        Should.Throw<ConfigurationException>(() => this.serializer.SerializeProperty(new GitVersionConfiguration(), path))
            .Message.ShouldContain("Unknown configuration property");

    [TestCase("")]
    [TestCase(".")]
    [TestCase("calculation..tag-prefix")]
    [TestCase("calculation.tag-prefix.")]
    [TestCase("Calculation.tag-prefix")]
    [TestCase("calculation.*")]
    [TestCase("calculation.strategies[0]")]
    [TestCase("calculation/tag-prefix")]
    public void RejectsMalformedPaths(string path) =>
        Should.Throw<ConfigurationException>(() => this.serializer.SerializeProperty(new GitVersionConfiguration(), path))
            .Message.ShouldContain("Invalid configuration property path");

    [TestCase("calculation")]
    [TestCase("output")]
    [TestCase("calculation.prevent-increment")]
    [TestCase("calculation.ignore")]
    [TestCase("calculation.branches")]
    [TestCase("calculation.branches.main.increment")]
    [TestCase("output.branches.missing.pre-release-weight")]
    [TestCase("calculation.merge-message-formats")]
    [TestCase("calculation.merge-message-formats.missing")]
    [TestCase("calculation.strategies")]
    [TestCase("calculation.strategies.0")]
    [TestCase("calculation.ignore.sha")]
    [TestCase("calculation.ignore.branches")]
    public void RejectsStructuredValuesAndDynamicTraversal(string path) =>
        Should.Throw<ConfigurationException>(() => this.serializer.SerializeProperty(new GitVersionConfiguration(), path))
            .Message.ShouldContain("only scalar properties of fixed objects");

    [Test]
    public void RejectsV6EvenForAValidV7Path()
    {
        SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, "v6");

        Should.Throw<ConfigurationException>(() => this.serializer.SerializeProperty(new GitVersionConfiguration(), "calculation.tag-prefix"))
            .Message.ShouldContain("require v7 configuration");
    }

    [Test]
    public void NonNullScalarLeavesMatchFullSerializedConfiguration()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();
        var document = this.serializer.Deserialize<Dictionary<object, object?>>(this.serializer.Serialize(configuration));

        AssertLeaves(document, "");
        return;

        void AssertLeaves(Dictionary<object, object?> node, string prefix)
        {
            foreach (var (key, value) in node)
            {
                if (key is "branches" or "merge-message-formats")
                {
                    continue;
                }
                var path = prefix + key;
                if (value is Dictionary<object, object?> child)
                {
                    AssertLeaves(child, path + ".");
                }
                else if (value is string || value is not IEnumerable)
                {
                    this.serializer.SerializeProperty(configuration, path).ShouldBe(JsonSerializer.Serialize(value), path);
                }
            }
        }
    }
}
