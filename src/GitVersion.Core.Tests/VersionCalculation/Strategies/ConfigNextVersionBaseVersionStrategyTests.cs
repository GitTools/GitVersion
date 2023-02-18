using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
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

    [TestCase("1.0.0", "1.0.0", SemanticVersionFormat.Strict)]
    [TestCase("1.0.0", "1.0.0", SemanticVersionFormat.Loose)]
    [TestCase("2.12.654651698", "2.12.654651698", SemanticVersionFormat.Strict)]
    [TestCase("2.12.654651698", "2.12.654651698", SemanticVersionFormat.Loose)]
    [TestCase("0.1", "0.1.0", SemanticVersionFormat.Loose)]
    public void ConfigNextVersionTest(string nextVersion, string expectedVersion, SemanticVersionFormat versionFormat)
    {
        var baseVersion = GetBaseVersion(new GitVersionConfiguration { NextVersion = nextVersion, SemanticVersionFormat = versionFormat });

        baseVersion.ShouldNotBeNull();
        baseVersion.ShouldIncrement.ShouldBe(false);
        baseVersion.SemanticVersion.ToString().ShouldBe(expectedVersion);
    }

    [TestCase("0.1", SemanticVersionFormat.Strict)]
    public void ConfigNextVersionTestShouldFail(string nextVersion, SemanticVersionFormat versionFormat)
        =>
            Should.Throw<WarningException>(()
                    => GetBaseVersion(new GitVersionConfiguration
                    {
                        NextVersion = nextVersion,
                        SemanticVersionFormat = versionFormat
                    }))
                .Message.ShouldBe($"Failed to parse {nextVersion} into a Semantic Version");


    private static BaseVersion? GetBaseVersion(GitVersionConfiguration? configuration = null)
    {
        var contextBuilder = new GitVersionContextBuilder();

        if (configuration != null)
        {
            contextBuilder = contextBuilder.WithConfig(configuration);
        }

        contextBuilder.Build();
        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var strategy = contextBuilder.ServicesProvider.GetServiceForType<IVersionStrategy, ConfigNextVersionVersionStrategy>();
        var context = contextBuilder.ServicesProvider.GetRequiredService<Lazy<GitVersionContext>>().Value;
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var branchConfiguration = context.Configuration.GetBranchConfiguration(branchMock);
        var effectiveConfiguration = new EffectiveConfiguration(context.Configuration, branchConfiguration);

        strategy.ShouldNotBeNull();
        return strategy.GetBaseVersions(new(branchMock, effectiveConfiguration)).SingleOrDefault();
    }
}
