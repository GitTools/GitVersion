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
        yaml.IndexOf("  z-last:", StringComparison.Ordinal)
            .ShouldBeLessThan(yaml.IndexOf("  a-first:", StringComparison.Ordinal));

        var firstBranchProperties = lines
            .SkipWhile(line => line != "  z-last:")
            .Skip(1)
            .TakeWhile(line => line.StartsWith("    ", StringComparison.Ordinal))
            .Select(GetPropertyName)
            .ToArray();

        firstBranchProperties.ShouldBe(firstBranchProperties.Order(StringComparer.Ordinal));
    }

    private static string GetPropertyName(string line) => line.TrimStart().Split(':', 2)[0];

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string? original = System.Environment.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);

        public EnvironmentVariableScope(string? value) =>
            System.Environment.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, value);

        public void Dispose() =>
            System.Environment.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, this.original);
    }
}
