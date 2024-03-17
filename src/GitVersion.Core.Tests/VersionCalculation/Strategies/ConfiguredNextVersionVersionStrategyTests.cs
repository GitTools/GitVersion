using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests.VersionCalculation.Strategies;

[TestFixture]
public class ConfiguredNextVersionVersionStrategyTests : TestBase
{
    [Test]
    public void ReturnsNullWhenNoNextVersionIsInConfig()
    {
        var baseVersion = GetBaseVersion();

        baseVersion.ShouldBe(null);
    }

    [TestCase("1.0.0", "1.0.0", SemanticVersionFormat.Strict, "1.0.0-1")]
    [TestCase("1.0.0", "1.0.0", SemanticVersionFormat.Loose, "1.0.0-1")]
    [TestCase("2.12.654651698", "2.12.654651698", SemanticVersionFormat.Strict, "2.12.654651698-1")]
    [TestCase("2.12.654651698", "2.12.654651698", SemanticVersionFormat.Loose, "2.12.654651698-1")]
    [TestCase("0.1", "0.1.0", SemanticVersionFormat.Loose, "0.1.0-1")]
    [TestCase("1.0.0-alpha.1", null, SemanticVersionFormat.Strict, null)]
    [TestCase("1.0.0-2", "1.0.0-2", SemanticVersionFormat.Strict, "1.0.0-2")]
    public void ConfiguredNextVersionTest(
        string nextVersion, string? semanticVersion, SemanticVersionFormat versionFormat, string? incrementedVersion)
    {
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "next-version", nextVersion },
            { "semantic-version-format", versionFormat }
        };
        var baseVersion = GetBaseVersion(overrideConfiguration);

        if (semanticVersion.IsNullOrEmpty())
        {
            baseVersion.ShouldBeNull();
            return;
        }

        baseVersion.ShouldNotBeNull();
        baseVersion.SemanticVersion.ToString().ShouldBe(semanticVersion);
        baseVersion.BaseVersionSource.ShouldBeNull();

        var shouldBeIncremented = semanticVersion != incrementedVersion;
        baseVersion.ShouldIncrement.ShouldBe(shouldBeIncremented);
        if (shouldBeIncremented)
        {
            baseVersion.Operator.ShouldNotBeNull();
            baseVersion.Operator!.Label.ShouldBe(string.Empty);
            baseVersion.Operator.ForceIncrement.ShouldBe(false);
            baseVersion.Operator.Increment.ShouldBe(VersionField.None);
            baseVersion.Operator.BaseVersionSource.ShouldBeNull();
        }
        else
        {
            baseVersion.Operator.ShouldBeNull();
        }
        baseVersion.GetIncrementedVersion().ToString().ShouldBe(incrementedVersion);
    }

    [TestCase("0.1", SemanticVersionFormat.Strict)]
    public void ConfiguredNextVersionTestShouldFail(string nextVersion, SemanticVersionFormat versionFormat)
    {
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "next-version", nextVersion },
            { "semantic-version-format", versionFormat }
        };

        Should.Throw<WarningException>(() => GetBaseVersion(overrideConfiguration))
            .Message.ShouldBe($"Failed to parse {nextVersion} into a Semantic Version");
    }

    private static BaseVersion? GetBaseVersion(IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        using var contextBuilder = new GitVersionContextBuilder().WithOverrideConfiguration(overrideConfiguration);
        contextBuilder.Build();
        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var strategy = contextBuilder.ServicesProvider.GetServiceForType<IVersionStrategy, ConfiguredNextVersionVersionStrategy>();
        var context = contextBuilder.ServicesProvider.GetRequiredService<Lazy<GitVersionContext>>().Value;
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());

        strategy.ShouldNotBeNull();
        return strategy.GetBaseVersions(context.Configuration.GetEffectiveBranchConfiguration(branchMock)).SingleOrDefault();
    }
}
