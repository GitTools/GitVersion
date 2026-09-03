namespace GitVersion.Configuration.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationSerializerTests
{
    private readonly ConfigurationSerializer serializer = new();

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
