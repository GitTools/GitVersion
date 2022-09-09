using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class IgnoreBeforeScenarios : TestBase
{
    [Test]
    public void ShouldFallbackToBaseVersionWhenAllCommitsAreIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();
        var dateTimeNow = DateTimeOffset.Now;
        var objectId = fixture.Repository.MakeACommit();

        var config = new ConfigurationBuilder().Add(new Config
        {
            Ignore = new IgnoreConfig
            {
                Before = dateTimeNow.AddDays(1)
            }
        }).Build();

        fixture.AssertFullSemver("0.0.1+0", config); // 0.0.1 becaus the main branch has the IncrementStrategy.Patch
    }

    [Test]
    public void ShouldFallbackToBaseVersionWhenAllCommitsAreIgnored2()
    {
        using var fixture = new EmptyRepositoryFixture();
        var dateTimeNow = DateTimeOffset.Now;
        var objectId = fixture.Repository.MakeACommit();

        var config = new ConfigurationBuilder().Add(new Config
        {
            Ignore = new IgnoreConfig
            {
                Before = dateTimeNow.AddDays(-1)
            }
        }).Build();

        fixture.AssertFullSemver("0.0.1+1", config); // 0.0.1 becaus the main branch has the IncrementStrategy.Patch
    }
}
