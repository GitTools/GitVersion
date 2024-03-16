using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class ConfiguredNextVersionScenarios : TestBase
{
    private static GitHubFlowConfigurationBuilder ConfigurationBuilder => GitHubFlowConfigurationBuilder.New
        .WithVersionStrategy(VersionStrategies.ConfiguredNextVersion)
        .WithBranch("main", _ => _.WithDeploymentMode(DeploymentMode.ManualDeployment));

    [Test]
    public void ShouldThrowGitVersionExceptionWhenAllCommitsAreIgnored1()
    {
        var configuration = ConfigurationBuilder.WithNextVersion("1.0.0").Build();

        using EmptyRepositoryFixture repositoryFixture = new("main");

        repositoryFixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        repositoryFixture.AssertFullSemver("1.0.0-1+1", configuration);
    }
}
