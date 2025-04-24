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

        Random random = new(4711);
        var semanticVersion = SemanticVersion.Empty;
        for (var i = 0; i < 500; i++)
        {
            var versionField = (VersionField)random.Next(1, 4);
            semanticVersion = semanticVersion.Increment(versionField, string.Empty, forceIncrement: true);
            fixture.MakeATaggedCommit(semanticVersion.ToString("j"));
        }

        fixture.BranchTo("feature");
        fixture.MakeACommit();

        var sw = Stopwatch.StartNew();

        fixture.AssertFullSemver("170.3.3-feature.1+1", configuration);
        sw.Stop();

        sw.ElapsedMilliseconds.ShouldBeLessThan(2500);
    }
}
