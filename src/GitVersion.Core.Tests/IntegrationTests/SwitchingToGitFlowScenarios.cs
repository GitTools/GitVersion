using GitVersion.Core.Tests.Helpers;
using GitVersion.Testing.Extensions;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class SwitchingToGitFlowScenarios : TestBase
{
    [Test]
    public void WhenDevelopBranchedFromMainWithLegacyVersionTagsDevelopCanUseReachableTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeCommits(5);
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(2);
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.AssertFullSemver("1.1.0-alpha.2");
    }
}
