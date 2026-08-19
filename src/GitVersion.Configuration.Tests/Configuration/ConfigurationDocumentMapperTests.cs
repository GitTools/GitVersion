namespace GitVersion.Configuration.Tests;

[TestFixture]
public class ConfigurationDocumentMapperTests
{
    [Test]
    public void AssignsEverySerializedPropertyToRootOrExactlyOneV7Section()
    {
        var serializedProperties = typeof(GitVersionConfiguration)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<JsonPropertyNameAttribute>() is not null)
            .ToArray();
        var calculationProperties = GetInterfacePropertyNames(typeof(ICalculationConfiguration));
        calculationProperties.Remove(nameof(ICalculationConfiguration.VersionStrategy));
        calculationProperties.Add(nameof(GitVersionConfiguration.VersionStrategies));
        var outputProperties = GetInterfacePropertyNames(typeof(IOutputConfiguration));

        calculationProperties.ShouldNotContain(nameof(GitVersionConfiguration.Workflow));
        outputProperties.ShouldNotContain(nameof(GitVersionConfiguration.Workflow));

        calculationProperties.Intersect(outputProperties)
            .ShouldBe([nameof(ICalculationConfiguration.Branches)]);
        calculationProperties.Union(outputProperties).Append(nameof(GitVersionConfiguration.Workflow)).Order()
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
            ["workflow"] = "GitHubFlow/v1",
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

        result["workflow"].ShouldBe("GitHubFlow/v1");
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

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void AcceptsEmptyAndWorkflowOnlyDocumentsInEitherVersion(bool v7, bool hasWorkflow)
    {
        Dictionary<object, object?> document = [];
        if (hasWorkflow)
        {
            document["workflow"] = "GitHubFlow/v1";
        }

        ConfigurationDocumentMapper.Detect(document).ShouldBe(hasWorkflow ? ConfigurationDocumentKind.Shared : ConfigurationDocumentKind.Empty);
        var normalized = ConfigurationDocumentMapper.Normalize(document, v7 ? ConfigurationVersion.V7 : ConfigurationVersion.V6, "test");
        var internalNormalized = ConfigurationDocumentMapper.NormalizeInternal(document, "template");

        normalized.ShouldBe(document);
        internalNormalized.ShouldBe(document);
    }

    [TestCase("calculation", true)]
    [TestCase("output", true)]
    [TestCase("tag-prefix", false)]
    public void RootWorkflowDoesNotDetermineDocumentVersion(string property, bool nested)
    {
        Dictionary<object, object?> document = new()
        {
            ["workflow"] = "GitHubFlow/v1",
            [property] = property == "tag-prefix" ? "v" : new Dictionary<object, object?>()
        };

        ConfigurationDocumentMapper.Detect(document).ShouldBe(nested ? ConfigurationDocumentKind.V7 : ConfigurationDocumentKind.V6);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RootWorkflowDoesNotMakeMixedDocumentValid(bool v7)
    {
        Dictionary<object, object?> document = new()
        {
            ["workflow"] = "GitHubFlow/v1",
            ["tag-prefix"] = "v",
            ["calculation"] = new Dictionary<object, object?>()
        };

        ConfigurationDocumentMapper.Detect(document).ShouldBe(ConfigurationDocumentKind.Mixed);
        Should.Throw<ConfigurationException>(() => ConfigurationDocumentMapper.Normalize(document, v7 ? ConfigurationVersion.V7 : ConfigurationVersion.V6, "test"))
            .Message.ShouldContain("unsupported root-level properties");
    }

    [TestCase("calculation", false)]
    [TestCase("calculation", true)]
    [TestCase("output", false)]
    [TestCase("output", true)]
    public void RejectsNestedWorkflowEvenWhenRootWorkflowExists(string section, bool hasRootWorkflow)
    {
        Dictionary<object, object?> document = new()
        {
            [section] = new Dictionary<object, object?> { ["workflow"] = "GitHubFlow/v1" }
        };
        if (hasRootWorkflow)
        {
            document["workflow"] = "GitHubFlow/v1";
        }

        var exception = Should.Throw<ConfigurationException>(() =>
            ConfigurationDocumentMapper.Normalize(document, ConfigurationVersion.V7, "test"));

        exception.Message.ShouldContain($"{section}.workflow");
        exception.Message.ShouldContain("root");
        exception.Message.ShouldContain("'workflow'");
    }

    [Test]
    public void BothNestingOverloadsKeepWorkflowAtRoot()
    {
        Dictionary<string, object?> typed = new()
        {
            ["workflow"] = "GitHubFlow/v1",
            ["tag-prefix"] = "v",
            ["update-build-number"] = false
        };
        var typedNested = ConfigurationDocumentMapper.Nest(typed);
        typedNested["workflow"].ShouldBe("GitHubFlow/v1");
        typedNested["calculation"].ShouldBeOfType<Dictionary<string, object?>>()
            .ContainsKey("workflow").ShouldBeFalse();

        var untypedNested = ConfigurationDocumentMapper.Nest(typed.ToDictionary(item => (object)item.Key, item => item.Value));
        untypedNested["workflow"].ShouldBe("GitHubFlow/v1");
        ConfigurationDocumentMapper.Flatten(untypedNested)
            .ShouldBe(typed.ToDictionary(item => (object)item.Key, item => item.Value));
    }

    private static HashSet<string> GetInterfacePropertyNames(Type interfaceType) =>
        interfaceType.GetInterfaces()
            .Append(interfaceType)
            .SelectMany(type => type.GetProperties())
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
}
