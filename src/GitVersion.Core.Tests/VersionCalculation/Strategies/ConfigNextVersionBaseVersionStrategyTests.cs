using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

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

    [TestCase("1.0.0", "1.0.0")]
    [TestCase("2.12.654651698", "2.12.654651698")]
    public void ConfigNextVersionTest(string nextVersion, string expectedVersion)
    {
        var baseVersion = GetBaseVersion(new Config
        {
            NextVersion = nextVersion
        });

        baseVersion.ShouldNotBeNull();
        baseVersion.ShouldIncrement.ShouldBe(false);
        baseVersion.SemanticVersion.ToString().ShouldBe(expectedVersion);
    }

    private static BaseVersion? GetBaseVersion(Config? config = null)
    {
        var contextBuilder = new GitVersionContextBuilder();

        if (config != null)
        {
            contextBuilder = contextBuilder.WithConfig(config);
        }

        contextBuilder.Build();
        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var strategy = contextBuilder.ServicesProvider.GetServiceForType<IVersionStrategy, ConfigNextVersionVersionStrategy>();
        var context = contextBuilder.ServicesProvider.GetRequiredService<Lazy<GitVersionContext>>().Value;
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var branchConfiguration = context.Configuration.GetBranchConfiguration(branchMock);
        var effectiveConfiguration = new EffectiveConfiguration(context.Configuration, branchConfiguration);
        return strategy.GetBaseVersions(new(branchMock, effectiveConfiguration)).SingleOrDefault();
    }
}
