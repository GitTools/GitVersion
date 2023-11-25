using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests.VersionCalculation.Strategies;

[TestFixture]
public class ConfigNextVersionBaseVersionStrategyTests : TestBase
{
    [Test]
    public void ReturnsNullWhenNoNextVersionIsInConfig()
    {
        var baseVersion = GetBaseVersion();

        baseVersion.ShouldBe(null);
    }

    [TestCase("1.0.0", "1.0.0", SemanticVersionFormat.Strict)]
    [TestCase("1.0.0", "1.0.0", SemanticVersionFormat.Loose)]
    [TestCase("2.12.654651698", "2.12.654651698", SemanticVersionFormat.Strict)]
    [TestCase("2.12.654651698", "2.12.654651698", SemanticVersionFormat.Loose)]
    [TestCase("0.1", "0.1.0", SemanticVersionFormat.Loose)]
    public void ConfigNextVersionTest(string nextVersion, string expectedVersion, SemanticVersionFormat versionFormat)
    {
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "next-version", nextVersion },
            { "semantic-version-format", versionFormat }
        };
        var baseVersion = GetBaseVersion(overrideConfiguration);

        baseVersion.ShouldNotBeNull();
        baseVersion.ShouldIncrement.ShouldBe(false);
        baseVersion.GetSemanticVersion().ToString().ShouldBe(expectedVersion);
    }

    [TestCase("0.1", SemanticVersionFormat.Strict)]
    public void ConfigNextVersionTestShouldFail(string nextVersion, SemanticVersionFormat versionFormat)
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
        var contextBuilder = new GitVersionContextBuilder().WithOverrideConfiguration(overrideConfiguration);
        contextBuilder.Build();
        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var strategy = contextBuilder.ServicesProvider.GetServiceForType<IVersionStrategy, ConfigNextVersionVersionStrategy>();
        var context = contextBuilder.ServicesProvider.GetRequiredService<Lazy<GitVersionContext>>().Value;
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());

        strategy.ShouldNotBeNull();
        return strategy.GetBaseVersions(context.Configuration.GetEffectiveBranchConfiguration(branchMock)).SingleOrDefault();
    }
}
