using GitVersion.VersionCalculation;

namespace GitVersion.Configuration.Tests;

[TestFixture]
public class ConfigurationSerializerTests
{
    private readonly ConfigurationSerializer serializer = new();

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
}
