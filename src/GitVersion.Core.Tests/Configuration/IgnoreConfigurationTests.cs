using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using NUnit.Framework;
using Shouldly;
using YamlDotNet.Core;

namespace GitVersion.Core.Tests.Configuration;

[TestFixture]
public class IgnoreConfigurationTests : TestBase
{
    [Test]
    public void CanDeserialize()
    {
        const string yaml = @"
ignore:
    sha: [b6c0c9fda88830ebcd563e500a5a7da5a1658e98]
    commits-before: 2015-10-23T12:23:15
";

        using var reader = new StringReader(yaml);
        var configuration = ConfigurationSerializer.Read(reader);

        configuration.Ignore.ShouldNotBeNull();
        configuration.Ignore.Shas.ShouldNotBeEmpty();
        configuration.Ignore.Shas.ShouldBe(new[] { "b6c0c9fda88830ebcd563e500a5a7da5a1658e98" });
        configuration.Ignore.Before.ShouldBe(DateTimeOffset.Parse("2015-10-23T12:23:15"));
    }

    [Test]
    public void ShouldSupportsOtherSequenceFormat()
    {
        const string yaml = @"
ignore:
    sha:
        - b6c0c9fda88830ebcd563e500a5a7da5a1658e98
        - 6c19c7c219ecf8dbc468042baefa73a1b213e8b1
";

        using var reader = new StringReader(yaml);
        var configuration = ConfigurationSerializer.Read(reader);

        configuration.Ignore.ShouldNotBeNull();
        configuration.Ignore.Shas.ShouldNotBeEmpty();
        configuration.Ignore.Shas.ShouldBe(new[] { "b6c0c9fda88830ebcd563e500a5a7da5a1658e98", "6c19c7c219ecf8dbc468042baefa73a1b213e8b1" });
    }

    [Test]
    public void WhenNotInConfigShouldHaveDefaults()
    {
        const string yaml = @"
next-version: 1.0
";

        using var reader = new StringReader(yaml);
        var configuration = ConfigurationSerializer.Read(reader);

        configuration.Ignore.ShouldNotBeNull();
        configuration.Ignore.Shas.ShouldBeEmpty();
        configuration.Ignore.Before.ShouldBe(null);
    }

    [Test]
    public void WhenBadDateFormatShouldFail()
    {
        const string yaml = @"
ignore:
    commits-before: bad format date
";

        using var reader = new StringReader(yaml);
        Should.Throw<YamlException>(() => ConfigurationSerializer.Read(reader));
    }

    [Test]
    public void NewInstanceShouldBeEmpty()
    {
        var ignoreConfig = new IgnoreConfiguration();

        ignoreConfig.IsEmpty.ShouldBeTrue();
    }
}
