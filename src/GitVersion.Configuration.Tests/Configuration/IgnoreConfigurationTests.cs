using System.Globalization;
using GitVersion.Tests;
using GitVersion.VersionCalculation;
using SharpYaml;

namespace GitVersion.Configuration.Tests;

[TestFixture]
public class IgnoreConfigurationTests : TestBase
{
    private readonly ConfigurationSerializer serializer = new();

    [Test]
    public void CanDeserialize()
    {
        const string yaml =
            """
            ignore:
                commits-before: 2015-10-23T12:23:15
                sha: [b6c0c9fda88830ebcd563e500a5a7da5a1658e98]
            """;

        var configuration = this.serializer.ReadConfiguration(yaml);

        configuration.ShouldNotBeNull();
        configuration.Ignore.ShouldNotBeNull();
        configuration.Ignore.Before.ShouldBe(DateTimeOffset.Parse("2015-10-23T12:23:15", CultureInfo.InvariantCulture));
        configuration.Ignore.Shas.ShouldNotBeEmpty();
        configuration.Ignore.Shas.ShouldBe(["b6c0c9fda88830ebcd563e500a5a7da5a1658e98"]);
    }

    [Test]
    public void ShouldSupportsOtherSequenceFormat()
    {
        const string yaml =
            """
            ignore:
                sha:
                    - b6c0c9fda88830ebcd563e500a5a7da5a1658e98
                    - 6c19c7c219ecf8dbc468042baefa73a1b213e8b1
            """;

        var configuration = this.serializer.ReadConfiguration(yaml);

        configuration.ShouldNotBeNull();
        configuration.Ignore.ShouldNotBeNull();
        configuration.Ignore.Shas.ShouldNotBeEmpty();
        configuration.Ignore.Shas.ShouldBe(["b6c0c9fda88830ebcd563e500a5a7da5a1658e98", "6c19c7c219ecf8dbc468042baefa73a1b213e8b1"]);
    }

    [Test]
    public void CanDeserializeCompactTagsSequence()
    {
        const string yaml = "ignore:\n  tags: ['^preview-', '^v0\\.']";

        var configuration = this.serializer.ReadConfiguration(yaml);

        configuration.ShouldNotBeNull();
        configuration.Ignore.Tags.ShouldBe(["^preview-", "^v0\\."]);
    }

    [Test]
    public void CanDeserializeMultilineTagsSequence()
    {
        const string yaml =
            """
            ignore:
                tags:
                    - ^preview-
                    - ^v0\.
            """;

        var configuration = this.serializer.ReadConfiguration(yaml);

        configuration.ShouldNotBeNull();
        configuration.Ignore.Tags.ShouldBe(["^preview-", "^v0\\."]);
    }

    [Test]
    public void WhenNotInConfigShouldHaveDefaults()
    {
        const string yaml = "next-version: 1.0";

        var configuration = this.serializer.ReadConfiguration(yaml);

        configuration.ShouldNotBeNull();
        configuration.Ignore.ShouldNotBeNull();
        configuration.Ignore.Before.ShouldBe(null);
        configuration.Ignore.Paths.ShouldBeEmpty();
        configuration.Ignore.Shas.ShouldBeEmpty();
        configuration.Ignore.Tags.ShouldBeEmpty();
    }

    [Test]
    public void IsEmpty_WhenTagPatternIsConfigured_ReturnsFalse()
    {
        var ignoreConfig = new IgnoreConfiguration { Tags = ["^preview-"] };

        ignoreConfig.IsEmpty.ShouldBeFalse();
    }

    [Test]
    public void InvalidIgnoreTagExpression_ThrowsConfigurationExceptionWithPropertyAndPattern()
    {
        const string invalidExpression = "[invalid";

        var exception = Should.Throw<ConfigurationException>(() => GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Tags = [invalidExpression] })
            .Build());

        exception.Message.ShouldContain("ignore.tags");
        exception.Message.ShouldContain(invalidExpression);
    }

    [Test]
    public void Serialize_IgnoreTagPatterns_UsesTagsPropertyName()
    {
        var ignoreConfig = new IgnoreConfiguration { Tags = ["^preview-"] };

        var yaml = this.serializer.Serialize(ignoreConfig);

        yaml.ShouldContain("tags:");
        yaml.ShouldContain("^preview-");
    }

    [Test]
    public void IgnoreConfigurationBuilder_WithTags_PreservesCollection()
    {
        var ignoreConfig = IgnoreConfigurationBuilder.New.WithTags("^preview-", "^v0\\.").Build();

        ignoreConfig.Tags.ShouldBe(["^preview-", "^v0\\."]);
    }

    [Test]
    public void WhenBadDateFormatShouldFail()
    {
        const string yaml =
            """
            ignore:
                commits-before: bad format date
            """;

        Should.Throw<YamlException>(() => this.serializer.ReadConfiguration(yaml));
    }

    [Test]
    public void ShouldSupportScalarVersionStrategiesOverrideFormat()
    {
        const string yaml = "strategies: ConfiguredNextVersion, TaggedCommit";

        var configuration = this.serializer.ReadConfiguration(yaml);

        configuration.ShouldNotBeNull();
        configuration.VersionStrategy.ShouldBe(VersionStrategies.ConfiguredNextVersion | VersionStrategies.TaggedCommit);
    }

    [Test]
    public void NewInstanceShouldBeEmpty()
    {
        var ignoreConfig = new IgnoreConfiguration();

        ignoreConfig.IsEmpty.ShouldBeTrue();
    }
}
