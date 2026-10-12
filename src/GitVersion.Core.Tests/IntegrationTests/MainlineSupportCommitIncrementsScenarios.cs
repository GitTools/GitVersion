using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class MainlineSupportCommitIncrementsScenarios : TestBase
{
    [TestCase(false, false, DeploymentMode.ContinuousDelivery, "0.0.4-3")]
    [TestCase(false, false, DeploymentMode.ContinuousDeployment, "0.0.4")]
    [TestCase(true, false, DeploymentMode.ContinuousDelivery, "0.0.4-3")]
    [TestCase(true, false, DeploymentMode.ContinuousDeployment, "0.0.4")]
    [TestCase(true, true, DeploymentMode.ContinuousDelivery, "0.0.5-4")]
    [TestCase(true, true, DeploymentMode.ContinuousDeployment, "0.0.5")]
    public void SupportMainlineProgressSurvivesMerge(bool tagged, bool postTagCommit,
        DeploymentMode mode, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        var mainTip = fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        var supportTip = tagged ? fixture.MakeATaggedCommit("0.0.4") : fixture.MakeACommit();
        if (postTagCommit)
        {
            fixture.MakeACommit();
        }
        var configuration = GetConfigurationBuilder().WithDeploymentMode(mode).Build();
        fixture.GetVersion(configuration).MajorMinorPatch.ShouldBe(postTagCommit ? "0.0.5" : "0.0.4");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(expectedVersion);
        version.CommitCountSourceSha.ShouldBe(mainTip);
        version.CommitCountSourceDistance.ShouldBe(postTagCommit ? "4" : "3");
        if (tagged)
        {
            version.SemVerSourceSha.ShouldBe(supportTip);
            version.SemVerSourceSemVer.ShouldBe("0.0.4");
            version.SemVerSourceIncrement.ShouldBe(postTagCommit ? "Patch" : "None");
        }
    }

    [TestCase("First API +semver: minor", "Second API +semver: minor", "0.2.0")]
    [TestCase("First API +semver: minor", "Ordinary patch", "0.1.1")]
    [TestCase("First API +semver: minor", "Reset =semver: none", "0.1.0")]
    public void OrderedSupportIncrementsArePreserved(string firstMessage, string secondMessage, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit(firstMessage);
        fixture.MakeACommit(secondMessage);
        var configuration = GetConfigurationBuilder().Build();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe(expectedVersion);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(expectedVersion);
    }

    [Test]
    public void RepeatedSupportMergesApplyOnlyNewIncrements()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder().Build();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe("0.0.4");
        var firstMerge = fixture.Repository.Head.Tip.Sha;
        fixture.Checkout("support/1.x");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("0.0.5");
        version.CommitCountSourceSha.ShouldBe(firstMerge);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [Test]
    public void SourcePreventionSuppressesAllSupportIncrements()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit("First API +semver: minor");
        fixture.MakeACommit("Second API +semver: major");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithPreventIncrementWhenBranchMerged(true)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("0.0.2");
    }

    [Test]
    public void FeatureCommitsStillContributeOneMergeIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("feature/work");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/work");

        fixture.GetVersion(GetConfigurationBuilder().Build()).FullSemVer.ShouldBe("0.0.3");
    }

    [TestCase(IncrementStrategy.None, "0.0.2")]
    [TestCase(IncrementStrategy.Patch, "0.0.4")]
    [TestCase(IncrementStrategy.Minor, "0.2.0")]
    [TestCase(IncrementStrategy.Major, "2.0.0")]
    [TestCase(IncrementStrategy.Inherit, "0.0.4")]
    public void SupportBranchIncrementAppliesToEachCommit(IncrementStrategy increment, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder().WithBranch("support", builder => builder.WithIncrement(increment)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(expectedVersion);
    }

    [TestCase(true, "2.0.1")]
    [TestCase(false, "3.0.0")]
    public void TaggedSourceBumpRespectsPrevention(bool preventTaggedIncrement, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit("Released API +semver: major");
        fixture.ApplyTag("1.0.5");
        fixture.Checkout(MainBranch);
        fixture.MakeATaggedCommit("2.0.0");
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithPreventIncrementWhenCurrentCommitTagged(preventTaggedIncrement)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(expectedVersion);
    }

    [Test]
    public void DeletedSupportRefRetainsEachIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        fixture.Repository.Branches.Remove("support/1.x");

        fixture.GetVersion(GetConfigurationBuilder().Build()).FullSemVer.ShouldBe("0.0.4");
    }

    [Test]
    public void FeatureMergedIntoSupportRetainsItsSingleContribution()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        var mainTip = fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.BranchTo("feature/work");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF("feature/work");
        fixture.MakeACommit();
        var configuration = GetConfigurationBuilder().Build();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe("0.0.5");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("0.0.5");
        version.CommitCountSourceSha.ShouldBe(mainTip);
        version.CommitCountSourceDistance.ShouldBe("6");
    }

    [TestCase(false, "0.0.5")]
    [TestCase(true, "0.0.7")]
    public void MainResumesAfterHigherSupportTag(bool addHigherAlternativeTag, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.MakeATaggedCommit("0.0.4");
        if (addHigherAlternativeTag)
        {
            fixture.ApplyTag("0.0.6-beta.1");
        }
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var merge = fixture.Repository.Head.Tip.Sha;
        fixture.MakeACommit();

        var version = fixture.GetVersion(GetConfigurationBuilder().Build());

        version.FullSemVer.ShouldBe(expectedVersion);
        version.CommitCountSourceSha.ShouldBe(merge);
        version.CommitCountSourceDistance.ShouldBe("1");
    }

    [TestCase(false, false, true, "1.1.1")]
    [TestCase(true, false, true, "1.1.1")]
    [TestCase(false, true, true, "1.1.2")]
    [TestCase(true, true, true, "1.1.2")]
    [TestCase(false, false, false, "1.1.2")]
    [TestCase(true, false, false, "1.1.2")]
    public void MainMergedIntoSupportIsNotReplayedWhenSupportReturns(bool tagMain, bool postMergeCommit,
        bool preventReceiverIncrement, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MakeACommit("Main API +semver: minor");
        var mainTip = fixture.Repository.Head.Tip.Sha;
        if (tagMain)
        {
            fixture.ApplyTag("1.1.0");
        }
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithPreventIncrementOfMergedBranch(preventReceiverIncrement)).Build();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe("1.1.0");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        if (postMergeCommit)
        {
            fixture.MakeACommit();
        }
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(expectedVersion);
        version.CommitCountSourceSha.ShouldBe(mainTip);
        version.CommitCountSourceDistance.ShouldBe(postMergeCommit ? "4" : "3");
    }

    private static GitFlowConfigurationBuilder GetConfigurationBuilder() => GitFlowConfigurationBuilder.New
        .WithVersionStrategies(VersionStrategies.Mainline)
        .WithDeploymentMode(DeploymentMode.ContinuousDeployment);
}
