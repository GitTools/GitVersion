using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class IgnoreBeforeScenarios : TestBase
{
    [TestCase(null, "0.0.1+0")]
    [TestCase("0.0.1", "0.0.1+0")]
    [TestCase("0.1.0", "0.1.0+0")]
    [TestCase("1.0.0", "1.0.0+0")]
    public void ShouldFallbackToBaseVersionWhenAllCommitsAreIgnored(string? nextVersion, string expectedFullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var dateTimeNow = DateTimeOffset.Now;
        fixture.MakeACommit();

        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion(nextVersion)
            .WithIgnoreConfiguration(new() { Before = dateTimeNow.AddDays(1) }).Build();

        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }

    [TestCase(null, "0.0.1+1")]
    [TestCase("0.0.1", "0.0.1+1")]
    [TestCase("0.1.0", "0.1.0+1")]
    [TestCase("1.0.0", "1.0.0+1")]
    public void ShouldNotFallbackToBaseVersionWhenAllCommitsAreNotIgnored(string? nextVersion, string expectedFullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var dateTimeNow = DateTimeOffset.Now;
        fixture.MakeACommit();

        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion(nextVersion)
            .WithIgnoreConfiguration(new() { Before = dateTimeNow.AddDays(-1) }).Build();

        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }
}
