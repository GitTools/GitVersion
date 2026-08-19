namespace GitVersion.Configuration.Tests;

[TestFixture]
public class ConfigurationDocumentMapperTests
{
    [Test]
    public void AssignsEverySerializedPropertyToExactlyOneV7Section()
    {
        var serializedProperties = typeof(GitVersionConfiguration)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<JsonPropertyNameAttribute>() is not null)
            .ToArray();
        var calculationProperties = GetInterfacePropertyNames(typeof(ICalculationConfiguration));
        calculationProperties.Remove(nameof(ICalculationConfiguration.VersionStrategy));
        calculationProperties.Add(nameof(GitVersionConfiguration.VersionStrategies));
        var outputProperties = GetInterfacePropertyNames(typeof(IOutputConfiguration));

        calculationProperties.Intersect(outputProperties)
            .ShouldBe([nameof(ICalculationConfiguration.Branches)]);
        calculationProperties.Union(outputProperties).Order()
            .ShouldBe(serializedProperties.Select(property => property.Name).Order());

        var expectedOutputPropertyNames = serializedProperties
            .Where(property => outputProperties.Contains(property.Name)
                && property.Name != nameof(IOutputConfiguration.Branches))
            .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name)
            .Order();
        serializedProperties
            .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name)
            .Where(ConfigurationDocumentMapper.IsOutputProperty)
            .Order()
            .ShouldBe(expectedOutputPropertyNames);

        var branchProperties = typeof(BranchConfiguration)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<JsonPropertyNameAttribute>() is not null)
            .ToArray();
        var calculationBranchProperties = GetInterfacePropertyNames(typeof(ICalculationBranchConfiguration));
        var outputBranchProperties = GetInterfacePropertyNames(typeof(IOutputBranchConfiguration));

        calculationBranchProperties.Intersect(outputBranchProperties).ShouldBeEmpty();
        calculationBranchProperties.Union(outputBranchProperties).Order()
            .ShouldBe(branchProperties.Select(property => property.Name).Order());
        branchProperties
            .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name)
            .Where(ConfigurationDocumentMapper.IsOutputBranchProperty)
            .Order()
            .ShouldBe(branchProperties
                .Where(property => outputBranchProperties.Contains(property.Name))
                .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name)
                .Order());
    }

    [Test]
    public void DetectsEmptyFlatNestedAndMixedDocuments()
    {
        ConfigurationDocumentMapper.Detect(new Dictionary<object, object?>())
            .ShouldBe(ConfigurationDocumentKind.Empty);
        ConfigurationDocumentMapper.Detect(new Dictionary<object, object?> { ["tag-prefix"] = "v" })
            .ShouldBe(ConfigurationDocumentKind.V6);
        ConfigurationDocumentMapper.Detect(new Dictionary<object, object?> { ["calculation"] = new Dictionary<object, object?>() })
            .ShouldBe(ConfigurationDocumentKind.V7);
        ConfigurationDocumentMapper.Detect(new Dictionary<object, object?>
        {
            ["calculation"] = new Dictionary<object, object?>(),
            ["tag-prefix"] = "v"
        })
            .ShouldBe(ConfigurationDocumentKind.Mixed);
    }

    [Test]
    public void FlattensAndMergesCalculationAndOutputBranches()
    {
        Dictionary<object, object?> document = new()
        {
            ["calculation"] = new Dictionary<object, object?>
            {
                ["tag-prefix"] = "v",
                ["branches"] = new Dictionary<object, object?>
                {
                    ["main"] = new Dictionary<object, object?> { ["increment"] = "Patch" }
                }
            },
            ["output"] = new Dictionary<object, object?>
            {
                ["update-build-number"] = false,
                ["branches"] = new Dictionary<object, object?>
                {
                    ["main"] = new Dictionary<object, object?> { ["pre-release-weight"] = 42 },
                    ["develop"] = new Dictionary<object, object?> { ["custom-version-format"] = "{SemVer}" }
                }
            }
        };

        var result = ConfigurationDocumentMapper.Flatten(document);

        result["tag-prefix"].ShouldBe("v");
        result["update-build-number"].ShouldBe(false);
        var branches = result["branches"].ShouldBeOfType<Dictionary<object, object?>>();
        var main = branches["main"].ShouldBeOfType<Dictionary<object, object?>>();
        main["increment"].ShouldBe("Patch");
        main["pre-release-weight"].ShouldBe(42);
        branches.ContainsKey("develop").ShouldBeTrue();
    }

    [Test]
    public void RejectsMixedAndSelectedVersionMismatches()
    {
        Dictionary<object, object?> flat = new() { ["tag-prefix"] = "v" };
        Dictionary<object, object?> nested = new() { ["calculation"] = new Dictionary<object, object?>() };
        Dictionary<object, object?> mixed = new()
        {
            ["calculation"] = new Dictionary<object, object?>(),
            ["tag-prefix"] = "v"
        };

        Should.Throw<ConfigurationException>(() =>
            ConfigurationDocumentMapper.Normalize(flat, ConfigurationVersion.V7, "test"));
        Should.Throw<ConfigurationException>(() =>
            ConfigurationDocumentMapper.Normalize(nested, ConfigurationVersion.V6, "test"));
        Should.Throw<ConfigurationException>(() =>
            ConfigurationDocumentMapper.Normalize(mixed, ConfigurationVersion.V7, "test"));
    }

    [Test]
    public void RejectsPropertiesInTheWrongV7SectionWithReplacement()
    {
        Dictionary<object, object?> wrongRoot = new()
        {
            ["calculation"] = new Dictionary<object, object?> { ["update-build-number"] = false }
        };
        Dictionary<object, object?> wrongBranch = new()
        {
            ["output"] = new Dictionary<object, object?>
            {
                ["branches"] = new Dictionary<object, object?>
                {
                    ["main"] = new Dictionary<object, object?> { ["increment"] = "Major" }
                }
            }
        };

        Should.Throw<ConfigurationException>(() => ConfigurationDocumentMapper.Flatten(wrongRoot))
            .Message.ShouldContain("output.update-build-number");
        Should.Throw<ConfigurationException>(() => ConfigurationDocumentMapper.Flatten(wrongBranch))
            .Message.ShouldContain("calculation.branches.<branch>.increment");
    }

    private static HashSet<string> GetInterfacePropertyNames(Type interfaceType) =>
        interfaceType.GetInterfaces()
            .Append(interfaceType)
            .SelectMany(type => type.GetProperties())
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
}
