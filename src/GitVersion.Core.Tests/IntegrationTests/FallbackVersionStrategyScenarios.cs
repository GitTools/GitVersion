using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class FallbackVersionStrategyScenarios : TestBase
{
    private static GitHubFlowConfigurationBuilder ConfigurationBuilder => GitHubFlowConfigurationBuilder.New
        .WithVersionStrategy(VersionStrategies.Fallback)
        .WithBranch("main", b => b.WithDeploymentMode(DeploymentMode.ManualDeployment));

    [TestCase(IncrementStrategy.None, "0.0.0-1+1")]
    [TestCase(IncrementStrategy.Patch, "0.0.1-1+1")]
    [TestCase(IncrementStrategy.Minor, "0.1.0-1+1")]
    [TestCase(IncrementStrategy.Major, "1.0.0-1+1")]
    public void EnsureVersionIncrementOnMainWillBeUsed(IncrementStrategy increment, string expected)
    {
        var configuration = ConfigurationBuilder
            .WithBranch("main", b => b.WithIncrement(increment))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver(expected, configuration);
    }

    [TestCase(IncrementStrategy.None, "0.0.0-1+1")]
    [TestCase(IncrementStrategy.Patch, "0.0.1-1+1")]
    [TestCase(IncrementStrategy.Minor, "0.1.0-1+1")]
    [TestCase(IncrementStrategy.Major, "1.0.0-1+1")]
    public void EnsureVersionIncrementOnMessageWillBeUsed(IncrementStrategy increment, string expected)
    {
        var configuration = ConfigurationBuilder
            .WithBranch("main", b => b.WithIncrement(IncrementStrategy.None))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit($"+semver: {increment}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver(expected, configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TakeTheLatestCommitAsBaseVersion(bool mode)
    {
        var configuration = ConfigurationBuilder
            .WithBranch("main", b => b
                .WithIncrement(IncrementStrategy.Major)
                .WithTrackMergeTarget(true)
                .WithTracksReleaseBranches(false)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.BranchTo("release/foo");
        fixture.Checkout("main");

        fixture.MakeACommit("B");
        if (mode)
        {
            fixture.MergeTo("release/foo");
            fixture.ApplyTag("0.0.0");
            fixture.Checkout("main");
        }
        else
        {
            fixture.ApplyTag("0.0.0");
        }

        fixture.MakeACommit("C");
        if (mode)
        {
            fixture.ApplyTag("0.0.1");
        }
        else
        {
            fixture.MergeTo("release/foo");
            fixture.ApplyTag("0.0.1");
            fixture.Checkout("main");
        }

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);
    }

    [TestCase(false, "1.0.0-1+4")]
    [TestCase(true, "1.0.0-1+2")]
    public void TakeTheCommitBranchedFromAsBaseVersionWhenTracksReleaseBranchesIsTrue(
        bool tracksReleaseBranches, string version)
    {
        var configuration = ConfigurationBuilder
            .WithBranch("main", b => b
                .WithIncrement(IncrementStrategy.Major)
                .WithTrackMergeTarget(false)
                .WithTracksReleaseBranches(tracksReleaseBranches)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.MakeACommit("B");
        fixture.BranchTo("release/foo");
        fixture.Checkout("main");
        fixture.MakeACommit("C");
        fixture.Checkout("release/foo");
        fixture.MakeACommit("D");
        fixture.ApplyTag("0.0.0");
        fixture.Checkout("main");
        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver(version, configuration);

        fixture.Repository.DumpGraph();
    }
}
