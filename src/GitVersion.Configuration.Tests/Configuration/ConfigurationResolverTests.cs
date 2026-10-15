using GitVersion.Tests;

namespace GitVersion.Configuration.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationResolverTests : TestBase
{
    private string? originalVersion;

    [SetUp]
    public void SetUp()
    {
        this.originalVersion = SysEnv.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);
        SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, "v7");
    }

    [TearDown]
    public void TearDown() => SysEnv.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, this.originalVersion);

    [TestCase("GitFlow/v1")]
    [TestCase("GitHubFlow/v1")]
    [TestCase("TrunkBased/preview1")]
    public void PureResolutionMatchesProviderWithFileAndOverrides(string workflow)
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var content = $"workflow: {workflow}\ncalculation:\n  tag-prefix: file-\noutput:\n  tag-pre-release-weight: 12";
            File.WriteAllText(Path.Combine(directory.FullName, "GitVersion.yml"), content);
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = directory.FullName });
            var sp = ConfigureServices(services => services.AddSingleton(options));
            var serializer = sp.GetRequiredService<IConfigurationSerializer>();
            var overrides = serializer.Deserialize<Dictionary<object, object?>>("calculation:\n  tag-prefix: runtime-\noutput:\n  tag-pre-release-weight: 0");

            var pure = sp.GetRequiredService<ConfigurationResolver>().Resolve(content, overrides);
            var normal = ((ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>()).ProvideForDirectory(directory.FullName, overrides);

            serializer.Serialize(pure).ShouldBe(serializer.Serialize(normal));
            pure.TagPrefixPattern.ShouldBe("runtime-");
            pure.TagPreReleaseWeight.ShouldBe(0);
            pure.Branches.Count.ShouldBeGreaterThan(0);
        }
        finally { directory.Delete(true); }
    }

    [TestCase("calculation:\n  missing: true", "calculation.missing")]
    [TestCase("calculation:\n  ignore:\n    missing: true", "calculation.ignore.missing")]
    [TestCase("calculation:\n  prevent-increment:\n    missing: true", "calculation.prevent-increment.missing")]
    [TestCase("calculation:\n  ignore:\n    paths: text", "calculation.ignore.paths")]
    [TestCase("calculation:\n  branches:\n    main:\n      source-branches: text", "calculation.branches.main.source-branches")]
    [TestCase("output:\n  branches:\n    main:\n      pre-release-weight: wrong", "output.branches.main.pre-release-weight")]
    [TestCase("calculation:\n  increment: Invalid", "calculation.increment")]
    [TestCase("calculation:\n  mode: 99", "calculation.mode")]
    [TestCase("calculation:\n  strategies: [Invalid]", "calculation.strategies")]
    [TestCase("calculation:\n  strategies: [99999]", "calculation.strategies")]
    [TestCase("output:\n  update-build-number: []", "output.update-build-number")]
    [TestCase("workflow: []", "workflow")]
    [TestCase("workflow: 123", "workflow")]
    [TestCase("calculation:\n  ignore:\n    commits-before: invalid-date", "calculation.ignore.commits-before")]
    [TestCase("calculation:\n  branches: null", "calculation.branches")]
    [TestCase("calculation:\n  ignore:\n    branches: [null]", "calculation.ignore.branches[0]")]
    public void InvalidPublicInputReportsThePropertyPath(string content, string path)
    {
        var resolver = new ConfigurationResolver(new ConfigurationSerializer());

        var error = Should.Throw<ConfigurationValidationException>(() => resolver.Resolve(content));

        error.Code.ShouldBe("GVCONFIG_PROPERTY");
        error.PropertyPath.ShouldBe(path);
    }

    [TestCase("tag-prefix")]
    [TestCase("version-in-branch-pattern")]
    [TestCase("major-version-bump-message")]
    [TestCase("minor-version-bump-message")]
    [TestCase("patch-version-bump-message")]
    [TestCase("no-bump-message")]
    [TestCase("version-bump-reset-message")]
    [TestCase("regex")]
    [TestCase("ignore.branches")]
    [TestCase("ignore.tags")]
    [TestCase("ignore.paths")]
    [TestCase("merge-message-formats.custom")]
    [TestCase("branches.main.regex")]
    public void AllConfiguredRegexesAreCheckedWithPublicPaths(string path)
    {
        var sections = path.Split('.');
        var lines = new List<string> { "calculation:" };
        for (var index = 0; index < sections.Length; index++)
        {
            var value = "";
            if (index == sections.Length - 1)
            {
                value = path.StartsWith("ignore.", StringComparison.Ordinal) ? " ['[']" : " '['";
            }
            lines.Add(new string(' ', (index + 1) * 2) + sections[index] + ":" + value);
        }
        var resolver = new ConfigurationResolver(new ConfigurationSerializer());

        var error = Should.Throw<ConfigurationValidationException>(() => resolver.Resolve(string.Join('\n', lines)));

        error.Code.ShouldBe("GVCONFIG_REGEX");
        error.PropertyPath.ShouldBe("calculation." + path);
        error.Message.ShouldNotContain("'['");
    }

    [Test]
    public void CustomBranchRelationshipsResolveWithoutRepositoryHistory()
    {
        // Workflows supply defaults but do not require any corresponding branch to exist in Git.
        var resolver = new ConfigurationResolver(new ConfigurationSerializer());
        var config = resolver.Resolve("calculation:\n  branches:\n    custom:\n      regex: '^custom/'\n      source-branches: [main]\n      is-source-branch-for: [feature]");

        config.Branches["feature"].SourceBranches.ShouldContain("custom");
        config.Branches["custom"].RegularExpression.ShouldBe("^custom/");
    }

    [TestCase("2.0.0", "Strict", true)]
    [TestCase("2.1", "Loose", true)]
    [TestCase("2.1", "Strict", false)]
    [TestCase("999999999999999999999999.0.0", "Strict", false)]
    public void NextVersionUsesTheRuntimeSemanticVersionFormat(string nextVersion, string format, bool valid)
    {
        var resolver = new ConfigurationResolver(new ConfigurationSerializer());
        var content = $"calculation:\n  next-version: {nextVersion}\n  semantic-version-format: {format}";
        if (valid)
        {
            resolver.Resolve(content).NextVersion.ShouldBe(nextVersion);
        }
        else
        {
            var error = Should.Throw<ConfigurationValidationException>(() => resolver.Resolve(content));
            error.Code.ShouldBe("GVCONFIG_VALUE");
            error.PropertyPath.ShouldBe("calculation.next-version");
            error.Message.ShouldNotContain(nextVersion);
        }
    }

    [Test]
    public void InternalOverridesCanUseTypedEnumsAndCollections()
    {
        var resolver = new ConfigurationResolver(new ConfigurationSerializer());
        var configuration = resolver.Resolve("{}", new Dictionary<object, object?>
        {
            ["semantic-version-format"] = SemanticVersionFormat.Loose,
            ["next-version"] = "2.1",
            ["branches"] = new Dictionary<object, object?>
            {
                ["main"] = new Dictionary<object, object?> { ["source-branches"] = new HashSet<string> { "develop" } }
            }
        });

        configuration.SemanticVersionFormat.ShouldBe(SemanticVersionFormat.Loose);
        configuration.NextVersion.ShouldBe("2.1");
        configuration.Branches["main"].SourceBranches.ShouldBe(["develop"]);
    }
}
