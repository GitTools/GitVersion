using GitVersion.Configuration;
using GitVersion.Testing.Extensions;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class IgnoreReferenceScenarios : TestBase
{
    [Test]
    public void GivenIgnoredTagAndEarlierEligibleTag_WhenCalculatingVersion_UsesEarlierTagAndCountsTargetCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeATaggedCommit("2.0.0");
        fixture.Repository.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Tags = ["^2\\.0\\.0$"] })
            .Build();

        fixture.AssertFullSemver("1.0.1-2", configuration);
    }

    [Test]
    public void GivenNonMatchingTagPattern_WhenCalculatingVersion_PreservesCurrentVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Tags = ["^preview-"] })
            .Build();

        fixture.AssertFullSemver("1.0.1-1", configuration);
    }
}
