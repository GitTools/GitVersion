using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class MainlineConfigurationInheritanceScenarios : TestBase
{
    [Test]
    public void FeatureMergedAfterItsSupportBaseInheritsTheReceivingMainIncrement()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
            .WithBranch("main", branch => branch.WithIncrement(IncrementStrategy.Patch))
            .WithBranch("support", branch => branch.WithIncrement(IncrementStrategy.Minor))
            .WithBranch("feature", branch => branch.WithIncrement(IncrementStrategy.Inherit))
            .Build();
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit("Support change");
        var supportBase = fixture.Repository.Head.Tip.Sha;
        fixture.BranchTo("feature/fix");
        fixture.MakeACommit("Feature change");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        fixture.AssertFullSemver("1.1.0", configuration);
        fixture.ApplyTag("1.1.0");
        var release = fixture.Repository.Head.Tip.Sha;
        fixture.MergeNoFF("feature/fix");
        var parents = fixture.Repository.Head.Tip.Parents.ToArray();
        fixture.Repository.ObjectDatabase.FindMergeBase(parents[0], parents[1]).ShouldNotBeNull().Sha.ShouldBe(supportBase);

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("1.1.1");
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.SemVerSourceSha.ShouldBe(release);
    }
}
