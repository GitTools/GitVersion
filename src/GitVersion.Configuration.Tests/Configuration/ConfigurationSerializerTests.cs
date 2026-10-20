namespace GitVersion.Configuration.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationSerializerTests
{
    private readonly ConfigurationSerializer serializer = new();

    [TestCase("[ConfiguredNextVersion, Mainline]")]
    [TestCase("ConfiguredNextVersion, Mainline")]
    [TestCase("[configured-next-version, mainline]")]
    public void DeserializesStrategyListsUsingGeneratedMetadata(string yaml)
    {
        var strategies = this.serializer.Deserialize<VersionCalculation.VersionStrategies[]>(yaml);

        strategies.ShouldBe([VersionCalculation.VersionStrategies.ConfiguredNextVersion, VersionCalculation.VersionStrategies.Mainline]);
        this.serializer.Deserialize<VersionCalculation.VersionStrategies[]>(ConfigurationSerializer.SerializeLegacy(strategies))
            .ShouldBe(strategies);
    }

    [Test]
    public void LegacySerializationRoundTripsTheGeneratedConfigurationModelGraph()
    {
        var configuration = new GitVersionConfiguration
        {
            TagPrefixPattern = "[vV]?",
            VersionStrategies = [VersionCalculation.VersionStrategies.TaggedCommit, VersionCalculation.VersionStrategies.Mainline],
            Ignore = new()
            {
                BeforeString = "2026-01-02T03:04:05Z",
                Paths = ["src/.*", "Unicode/é"],
                Shas = ["abc123"]
            },
            PreventIncrement = new() { OfMergedBranch = true, WhenCurrentCommitTagged = false },
            Branches = new()
            {
                ["feature/custom"] = new()
                {
                    Increment = IncrementStrategy.Minor,
                    PreventIncrement = new() { WhenBranchMerged = true },
                    SourceBranches = ["main", "release"]
                }
            }
        };

        var yaml = ConfigurationSerializer.SerializeLegacy(configuration);
        var roundTrip = ConfigurationSerializer.DeserializeLegacyConfiguration(yaml).ShouldNotBeNull();

        roundTrip.TagPrefixPattern.ShouldBe("[vV]?");
        roundTrip.VersionStrategies.ShouldBe(configuration.VersionStrategies);
        roundTrip.Ignore.Before.ShouldBe(configuration.Ignore.Before);
        roundTrip.Ignore.Paths.ShouldBe(configuration.Ignore.Paths, ignoreOrder: true);
        roundTrip.Ignore.Shas.ShouldBe(configuration.Ignore.Shas, ignoreOrder: true);
        roundTrip.PreventIncrement.OfMergedBranch.ShouldBe(true);
        roundTrip.PreventIncrement.WhenCurrentCommitTagged.ShouldBe(false);
        roundTrip.Branches["feature/custom"].Increment.ShouldBe(IncrementStrategy.Minor);
        roundTrip.Branches["feature/custom"].PreventIncrement.WhenBranchMerged.ShouldBe(true);
        roundTrip.Branches["feature/custom"].SourceBranches.ShouldBe(["main", "release"], ignoreOrder: true);
        yaml.ShouldNotContain("calculation:");
        yaml.ShouldNotContain("is-empty:");
        yaml.ShouldNotContain("  before:");
    }

    [Test]
    public void LegacySerializationPreservesExplicitNullsInUntypedDocuments()
    {
        Dictionary<object, object?> document = new()
        {
            ["next-version"] = null,
            ["branches"] = new Dictionary<object, object?>
            {
                ["feature"] = new Dictionary<object, object?> { ["pre-release-weight"] = null }
            }
        };

        var yaml = ConfigurationSerializer.SerializeLegacy(document);
        var roundTrip = this.serializer.Deserialize<Dictionary<object, object?>>(yaml);

        roundTrip.ContainsKey("next-version").ShouldBeTrue();
        roundTrip["next-version"].ShouldBeNull();
        var branches = roundTrip["branches"].ShouldBeOfType<Dictionary<object, object?>>();
        var feature = branches["feature"].ShouldBeOfType<Dictionary<object, object?>>();
        feature.ContainsKey("pre-release-weight").ShouldBeTrue();
        feature["pre-release-weight"].ShouldBeNull();
    }

    [Test]
    public void LegacySerializationConvertsObjectKeyedMapsInsideStringMapsAndSequences()
    {
        Dictionary<string, object?> document = new()
        {
            ["values"] = new List<object?>
            {
                new Dictionary<object, object?> { ["name"] = "café", ["nullable"] = null },
                new Dictionary<string, object?>
                {
                    ["nested"] = new Dictionary<object, object?> { ["enabled"] = false }
                }
            }
        };

        var yaml = ConfigurationSerializer.SerializeLegacy(document);
        var roundTrip = this.serializer.Deserialize<Dictionary<object, object?>>(yaml);
        var values = roundTrip["values"].ShouldBeOfType<List<object?>>();
        var first = values[0].ShouldBeOfType<Dictionary<object, object?>>();
        first["name"].ShouldBe("café");
        first.ContainsKey("nullable").ShouldBeTrue();
        first["nullable"].ShouldBeNull();
        values[1].ShouldBeOfType<Dictionary<object, object?>>()
            ["nested"].ShouldBeOfType<Dictionary<object, object?>>()
            ["enabled"].ShouldBe(false);
    }

    [Test]
    public void LegacySerializationRejectsNonStringObjectKeys()
    {
        Dictionary<object, object?> document = new() { [42] = "invalid" };

        Should.Throw<SharpYaml.YamlException>(() => ConfigurationSerializer.SerializeLegacy(document))
            .Message.ShouldContain("mapping keys must be strings");
    }

    [Test]
    public void SerializesV7CalculationAndOutputSectionsAndRoundTrips()
    {
        using var scope = new EnvironmentVariableScope("v7");
        var configuration = new GitVersionConfiguration
        {
            TagPrefixPattern = "custom-",
            UpdateBuildNumber = false,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                ["main"] = new() { Increment = IncrementStrategy.Major, PreReleaseWeight = 42 }
            }
        };

        var yaml = this.serializer.Serialize(configuration);
        var roundTrip = ConfigurationSerializer.ReadConfiguration(yaml);

        yaml.ShouldContain("calculation:");
        yaml.ShouldContain("output:");
        yaml.ShouldContain("  tag-prefix: custom-");
        yaml.ShouldContain("    main:");
        roundTrip.ShouldNotBeNull();
        roundTrip.TagPrefixPattern.ShouldBe("custom-");
        roundTrip.UpdateBuildNumber.ShouldBeFalse();
        roundTrip.Branches["main"].Increment.ShouldBe(IncrementStrategy.Major);
        roundTrip.Branches["main"].PreReleaseWeight.ShouldBe(42);
    }

    [Test]
    public void SerializesFlatConfigurationWhenV6IsSelected()
    {
        using var scope = new EnvironmentVariableScope("v6");
        var configuration = new GitVersionConfiguration { TagPrefixPattern = "custom-" };

        var yaml = this.serializer.Serialize(configuration);

        yaml.ShouldContain("tag-prefix: custom-");
        yaml.ShouldNotContain("calculation:");
        yaml.ShouldNotContain("output:");
    }

    [Test]
    public void Serialize_OrdersConfigurationPropertiesAndPreservesBranchNames()
    {
        var configuration = new GitVersionConfiguration
        {
            Branches = new Dictionary<string, BranchConfiguration>
            {
                ["z-last"] = new() { Label = "z", Increment = IncrementStrategy.Minor },
                ["a-first"] = new() { Label = "a", Increment = IncrementStrategy.Major }
            }
        };

        var yaml = this.serializer.Serialize(configuration);
        var lines = yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var rootPropertyNames = lines
            .Where(line => !char.IsWhiteSpace(line[0]))
            .Select(GetPropertyName)
            .ToArray();

        rootPropertyNames.ShouldBe(rootPropertyNames.Order(StringComparer.Ordinal));
        var firstBranchIndex = Array.FindIndex(lines, line => line.TrimStart() == "z-last:");
        var secondBranchIndex = Array.FindIndex(lines, line => line.TrimStart() == "a-first:");
        firstBranchIndex.ShouldBeGreaterThanOrEqualTo(0);
        firstBranchIndex.ShouldBeLessThan(secondBranchIndex);

        var branchIndentation = GetIndentation(lines[firstBranchIndex]);
        var firstBranchProperties = lines
            .Skip(firstBranchIndex + 1)
            .TakeWhile(line => GetIndentation(line) > branchIndentation)
            .Where(line => GetIndentation(line) == branchIndentation + 2)
            .Select(GetPropertyName)
            .ToArray();

        firstBranchProperties.ShouldNotBeEmpty();
        firstBranchProperties.ShouldBe(firstBranchProperties.Order(StringComparer.Ordinal));
    }

    [Test]
    public void ReusesConfigurationFacadesAndBranchProjections()
    {
        var configuration = new GitVersionConfiguration
        {
            Branches = new Dictionary<string, BranchConfiguration> { ["main"] = new() }
        };
        var effectiveConfiguration = (IGitVersionConfiguration)configuration;

        configuration.Calculation.ShouldBeSameAs(configuration.Calculation);
        configuration.Output.ShouldBeSameAs(configuration.Output);
        configuration.Calculation.Branches.ShouldBeSameAs(configuration.Calculation.Branches);
        configuration.Output.Branches.ShouldBeSameAs(configuration.Output.Branches);
        effectiveConfiguration.Branches.ShouldBeSameAs(effectiveConfiguration.Branches);
    }

    private static string GetPropertyName(string line) => line.TrimStart().Split(':', 2)[0];

    private static int GetIndentation(string line) => line.Length - line.TrimStart().Length;

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string? original = System.Environment.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);

        public EnvironmentVariableScope(string? value) =>
            System.Environment.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, value);

        public void Dispose() =>
            System.Environment.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, this.original);
    }
}
