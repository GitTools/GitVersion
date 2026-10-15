using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class MainlineBranchMergeScenarios : TestBase
{
    [TestCase(false, false, "1.0.0", "2.0.0", "2.0.0")]
    [TestCase(true, false, "1.0.0", "2.0.0", "2.0.0")]
    [TestCase(false, true, "1.0.0", "2.0.0", "2.0.0")]
    [TestCase(true, true, "1.0.0", "2.0.0", "2.0.0")]
    [TestCase(false, false, "2.0.0", "2.0.0", "2.0.0")]
    [TestCase(true, false, "2.0.0", "2.0.0", "2.0.0")]
    public void ArbitraryMainlinesSelectHighestVersion(bool reverse, bool deleteSource,
        string leftVersion, string rightVersion, string expected)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("0.1.0");
        fixture.BranchTo("production/left");
        var leftTag = fixture.MakeATaggedCommit(leftVersion);
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        var rightTag = fixture.MakeATaggedCommit($"v{rightVersion}");
        var receiver = reverse ? "production/left" : "production/right";
        var source = reverse ? "production/right" : "production/left";
        fixture.Checkout(receiver);
        fixture.MergeNoFF(source);
        if (deleteSource)
        {
            fixture.Repository.Branches.Remove(source);
        }

        var version = fixture.GetVersion(GetConfiguration());

        version.FullSemVer.ShouldBe(expected);
        version.SemVerSourceSha.ShouldBe(rightVersion == leftVersion && reverse ? leftTag : rightTag);
        version.SemVerSourceIncrement.ShouldBe("None");
        version.CommitCountSourceSha.ShouldBe(reverse ? leftTag : rightTag);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OlderReleaseLineDoesNotReplayItsIncrementsOntoTheWinner(bool reverse)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/left");
        fixture.MakeACommit("First API +semver: minor");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        var winningTag = fixture.MakeATaggedCommit("2.0.0");
        if (reverse)
        {
            fixture.Checkout("production/left");
        }
        fixture.MergeNoFF(reverse ? "production/right" : "production/left");

        var version = fixture.GetVersion(GetConfiguration());

        version.FullSemVer.ShouldBe("2.0.0");
        version.SemVerSourceSha.ShouldBe(winningTag);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IndependentlyComputedProgressWinsInEitherDirection(bool reverse)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/left");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        fixture.MakeACommit();
        if (reverse)
        {
            fixture.Checkout("production/left");
        }
        fixture.MergeNoFF(reverse ? "production/right" : "production/left");

        fixture.GetVersion(GetConfiguration()).FullSemVer.ShouldBe("1.0.3");
    }

    [TestCase(true, false, "3.0.0")]
    [TestCase(false, false, "3.0.0")]
    [TestCase(true, true, "2.0.0")]
    [TestCase(false, true, "2.0.1")]
    public void MergeControlsApplyToIndependentVersions(bool preventReceiver, bool preventSource, string expected)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/left");
        fixture.MakeATaggedCommit("2.0.0");
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        fixture.MakeACommit("First API +semver: major");
        fixture.MakeACommit("Second API +semver: major");
        fixture.Checkout("production/left");
        fixture.MergeNoFF("production/right");
        var configuration = GetConfigurationBuilder()
            .WithBranch("left", b => b.WithPreventIncrementOfMergedBranch(preventReceiver))
            .WithBranch("right", b => b.WithPreventIncrementWhenBranchMerged(preventSource)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(expected);
    }

    [Test]
    public void HigherReachableTagRemainsAFloorWhenSourceIncrementsArePrevented()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/left");
        fixture.MakeATaggedCommit("2.0.0");
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        fixture.MakeATaggedCommit("3.0.0");
        fixture.Checkout("production/left");
        fixture.MergeNoFF("production/right");
        var configuration = GetConfigurationBuilder()
            .WithBranch("right", b => b.WithPreventIncrementWhenBranchMerged(true)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("3.0.0");
    }

    [Test]
    public void ExplicitMergeMessageCanAdvanceTheReceiver()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/left");
        fixture.MakeATaggedCommit("2.0.0");
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        fixture.MakeACommit();
        fixture.Checkout("production/left");
        fixture.Repository.MergeNoFF("production/right", "Merge branch 'production/right' +semver: major");

        fixture.GetVersion(GetConfiguration()).FullSemVer.ShouldBe("3.0.0");
    }

    [Test]
    public void SupportBranchesUseTheSameHighestVersionRule()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/one");
        fixture.MakeATaggedCommit("1.1.0");
        fixture.Checkout(MainBranch);
        fixture.BranchTo("support/two");
        fixture.MakeATaggedCommit("1.2.0");
        fixture.MergeNoFF("support/one");

        fixture.GetVersion(GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment).Build()).FullSemVer.ShouldBe("1.2.0");
    }

    [Test]
    public void RepeatedRoundTripsKeepIndependentProgress()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/left");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        fixture.MakeATaggedCommit("2.0.0");
        var configuration = GetConfiguration();

        for (var i = 1; i <= 12; i++)
        {
            fixture.Checkout("production/left");
            fixture.MergeNoFF("production/right");
            fixture.MakeACommit();
            fixture.Checkout("production/right");
            fixture.MergeNoFF("production/left");

            fixture.GetVersion(configuration).FullSemVer.ShouldBe($"2.0.{i}");
        }

        fixture.GetVersion(configuration).CommitCountSourceDistance.ShouldBe("3");
    }

    [Test]
    public void SourceTagUsesItsOwnBranchLabel()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/left");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/right");
        fixture.MakeACommit("First API +semver: major");
        fixture.MakeACommit("Released API +semver: major");
        var sourceTag = fixture.Repository.Head.Tip.Sha;
        fixture.ApplyTag("2.0.0-beta.2");
        fixture.Checkout("production/left");
        fixture.MergeNoFF("production/right");
        var configuration = GetConfigurationBuilder()
            .WithBranch("left", b => b.WithLabel("alpha"))
            .WithBranch("right", b => b.WithLabel("beta")).Build();

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("2.0.0");
        version.SemVerSourceSha.ShouldBe(sourceTag);
    }

    [TestCase(DeploymentMode.ManualDeployment, false, "2.0.0-alpha.6+2")]
    [TestCase(DeploymentMode.ManualDeployment, true, "2.0.0-alpha.6+2")]
    [TestCase(DeploymentMode.ContinuousDelivery, false, "2.0.0-alpha.7")]
    [TestCase(DeploymentMode.ContinuousDelivery, true, "2.0.0-alpha.7")]
    [TestCase(DeploymentMode.ContinuousDeployment, false, "2.0.0")]
    [TestCase(DeploymentMode.ContinuousDeployment, true, "2.0.0")]
    public void ReceiverPrereleaseSequenceIsPreserved(DeploymentMode mode, bool preventSource, string expected)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("production/right");
        fixture.MakeATaggedCommit("1.0.1");
        fixture.Checkout(MainBranch);
        fixture.BranchTo("production/left");
        var receiverTag = fixture.MakeATaggedCommit("2.0.0-alpha.5");
        fixture.MergeNoFF("production/right");
        var configuration = GetConfigurationBuilder().WithDeploymentMode(mode)
            .WithBranch("left", b => b.WithLabel("alpha"))
            .WithBranch("right", b => b.WithPreventIncrementWhenBranchMerged(preventSource)).Build();

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(expected);
        version.SemVerSourceSha.ShouldBe(receiverTag);
        version.CommitCountSourceSha.ShouldBe(receiverTag);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    private static IGitVersionConfiguration GetConfiguration() => GetConfigurationBuilder().Build();

    private static GitFlowConfigurationBuilder GetConfigurationBuilder() => GitFlowConfigurationBuilder.New
        .WithVersionStrategies(VersionStrategies.Mainline)
        .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
        .WithBranch("left", b => b.WithRegularExpression("^production/left$").WithIsMainBranch(true)
            .WithLabel("").WithIncrement(IncrementStrategy.Patch).WithPreventIncrementOfMergedBranch(true))
        .WithBranch("right", b => b.WithRegularExpression("^production/right$").WithIsMainBranch(true)
            .WithLabel("").WithIncrement(IncrementStrategy.Patch).WithPreventIncrementOfMergedBranch(true));
}
