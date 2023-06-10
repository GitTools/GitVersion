using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests.IntegrationTests;

public class PerformanceScenarios : TestBase
{
    [Test]
    public void RepositoryWithALotOfTags()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        const int maxCommits = 500;
        for (int i = 0; i < maxCommits; i++)
        {
            fixture.MakeATaggedCommit($"1.0.{i}");
        }

        fixture.BranchTo("feature");
        fixture.MakeACommit();

        var sw = Stopwatch.StartNew();

        fixture.AssertFullSemver($"1.0.{maxCommits}-feature.1+1", configuration);
        sw.ElapsedMilliseconds.ShouldBeLessThan(5000);
    }
}
