using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class MainlineSupportMergeScenarios : TestBase
{
    [TestCase(DeploymentMode.ManualDeployment, "2.0.1-1+2")]
    [TestCase(DeploymentMode.ContinuousDelivery, "2.0.1-2")]
    [TestCase(DeploymentMode.ContinuousDeployment, "2.0.1")]
    public void TaggedSupportMergedIntoMainKeepsMainVersionAndSources(DeploymentMode mode, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeATaggedCommit("1.0.5");
        fixture.Checkout(MainBranch);
        var mainTagSha = fixture.MakeATaggedCommit("2.0.0");
        fixture.MergeNoFF("support/1.x");

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(mode)
            .Build();
        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(fullSemVer);
        version.MajorMinorPatch.ShouldBe("2.0.1");
        version.SemVerSourceSemVer.ShouldBe("2.0.0");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [TestCase(DeploymentMode.ContinuousDelivery, "0.0.2-2")]
    [TestCase(DeploymentMode.ContinuousDeployment, "0.0.2")]
    public void Issue4057UntaggedSupportMergeCalculatesVersion(DeploymentMode mode, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(mode)
            .Build();
        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(fullSemVer);
        version.MajorMinorPatch.ShouldBe("0.0.2");
    }

    [Test]
    public void UntaggedSupportMergeIncrementsMainOnce()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.MergeNoFF("support/1.x");

        fixture.GetVersion(GetConfigurationBuilder().Build()).FullSemVer.ShouldBe("0.0.3");
    }

    [Test]
    public void MainAndSupportAdvancingUseMainVersionAsBaseline()
    {
        using var fixture = new EmptyRepositoryFixture();
        var mainTagSha = CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        var mainCommitSha = fixture.MakeACommit();
        fixture.MergeNoFF("support/1.x");

        var version = fixture.GetVersion(GetConfigurationBuilder().Build());

        version.FullSemVer.ShouldBe("2.0.2");
        version.SemVerSourceSemVer.ShouldBe("2.0.1-1");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(mainCommitSha);
        version.CommitCountSourceDistance.ShouldBe("3");
    }

    [Test]
    public void RepeatedSupportMergesDoNotCountPreviouslyMergedWorkAgain()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MakeACommit("Add API +semver: minor");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var firstMergeSha = fixture.Repository.Head.Tip.Sha;
        fixture.GetVersion(GetConfigurationBuilder().Build()).FullSemVer.ShouldBe("2.1.0");

        fixture.Checkout("support/1.x");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        var version = fixture.GetVersion(GetConfigurationBuilder().Build());
        version.FullSemVer.ShouldBe("2.1.1");
        version.CommitCountSourceSha.ShouldBe(firstMergeSha);
        version.CommitCountSourceDistance.ShouldBe("2");

        fixture.MakeACommit();
        fixture.GetVersion(GetConfigurationBuilder().Build()).FullSemVer.ShouldBe("2.1.2");
    }

    [TestCase(IncrementStrategy.None, "2.0.0")]
    [TestCase(IncrementStrategy.Patch, "2.0.1")]
    [TestCase(IncrementStrategy.Minor, "2.1.0")]
    [TestCase(IncrementStrategy.Major, "3.0.0")]
    [TestCase(IncrementStrategy.Inherit, "2.0.1")]
    public void SupportBranchIncrementControlsMergeContribution(IncrementStrategy increment, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithIncrement(increment)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(fullSemVer);
    }

    [TestCase(true, false, "2.0.1")]
    [TestCase(false, false, "2.1.0")]
    [TestCase(true, true, "2.0.0")]
    [TestCase(false, true, "2.1.0")]
    public void MergePreventionControlsReceiverAndSupportIncrements(
        bool preventReceiverIncrement, bool preventSupportIncrement, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithIncrement(IncrementStrategy.Minor)
                .WithPreventIncrementOfMergedBranch(preventReceiverIncrement))
            .WithBranch("support", builder => builder.WithPreventIncrementWhenBranchMerged(preventSupportIncrement))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(fullSemVer);
    }

    [TestCase(CommitMessageIncrementMode.Enabled, "2.1.0")]
    [TestCase(CommitMessageIncrementMode.MergeMessageOnly, "2.0.1")]
    [TestCase(CommitMessageIncrementMode.Disabled, "2.0.1")]
    public void SupportCommitMessageModeControlsBump(CommitMessageIncrementMode mode, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MakeACommit("Add API +semver: minor");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithCommitMessageIncrementing(mode)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(fullSemVer);
    }

    [Test]
    public void TaggedSupportHistoryDoesNotReplayItsCommitMessageBump()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit("Change API +semver: major");
        fixture.ApplyTag("1.0.5");
        fixture.Checkout(MainBranch);
        fixture.MakeATaggedCommit("2.0.0");
        fixture.MergeNoFF("support/1.x");

        fixture.GetVersion(GetConfigurationBuilder().Build()).FullSemVer.ShouldBe("2.0.1");
    }

    [Test]
    public void ExplicitSupportIncrementKeepsItsOwnCommitMessageMode()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MakeACommit("Add API +semver: minor");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithCommitMessageIncrementing(CommitMessageIncrementMode.Disabled))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.1.0");
    }

    [Test]
    public void SupportBumpResetSuppressesEarlierBumpsAndBranchDefault()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MakeACommit("Change API +semver: major");
        fixture.MakeACommit("Reset =semver: none");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        fixture.GetVersion(GetConfigurationBuilder().Build()).FullSemVer.ShouldBe("2.0.0");
    }

    [Test]
    public void MergeMessageBumpAppliesEvenWhenBothBranchIncrementsArePrevented()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Repository.MergeNoFF("support/1.x", "Merge branch 'support/1.x' +semver: major");
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithPreventIncrementWhenBranchMerged(true)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("3.0.0");
    }

    [Test]
    public void SupportMergeMessageModeIncludesNestedMergeMessages()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.BranchTo("feature/fix");
        fixture.MakeACommit();
        fixture.Checkout("support/1.x");
        fixture.Repository.MergeNoFF("feature/fix", "Merge branch 'feature/fix' +semver: minor");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithCommitMessageIncrementing(CommitMessageIncrementMode.MergeMessageOnly))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.1.0");
    }

    [Test]
    public void DeletedSupportRefDoesNotChangeMergeVersionOrCountingSource()
    {
        using var fixture = new EmptyRepositoryFixture();
        var mainTagSha = CreateTaggedBranches(fixture);
        fixture.MergeNoFF("support/1.x");
        fixture.Repository.Branches.Remove("support/1.x");

        var version = fixture.GetVersion(GetConfigurationBuilder().Build());
        version.FullSemVer.ShouldBe("2.0.1");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [Test]
    public void TaggedMergeUsesCurrentTagThenMainlineResumesAfterIt()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.MergeNoFF("support/1.x");
        fixture.ApplyTag("2.0.1");
        var mergeSha = fixture.Repository.Head.Tip.Sha;
        var configuration = GetConfigurationBuilder().Build();

        var taggedVersion = fixture.GetVersion(configuration);
        taggedVersion.FullSemVer.ShouldBe("2.0.1");
        taggedVersion.SemVerSourceSha.ShouldBe(mergeSha);
        taggedVersion.CommitCountSourceDistance.ShouldBe("0");

        fixture.MakeACommit();
        var nextVersion = fixture.GetVersion(configuration);
        nextVersion.FullSemVer.ShouldBe("2.0.2");
        nextVersion.SemVerSourceSha.ShouldBe(mergeSha);
        nextVersion.CommitCountSourceDistance.ShouldBe("1");
    }

    [Test]
    public void OriginalBranchOnlyScenarioRetainsItsVersions()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("support/1.x");
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("0.0.2-0");
        fixture.MakeACommit();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe("0.0.2-1");
    }

    [TestCase(DeploymentMode.ManualDeployment, "2.0.1-1+2")]
    [TestCase(DeploymentMode.ContinuousDelivery, "2.0.1-2")]
    [TestCase(DeploymentMode.ContinuousDeployment, "2.0.1")]
    public void TaggedMainMergedIntoSupportUsesMainVersionAndSources(DeploymentMode mode, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var mainTagSha = CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var configuration = GetConfigurationBuilder().WithDeploymentMode(mode).Build();

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(fullSemVer);
        version.MajorMinorPatch.ShouldBe("2.0.1");
        version.SemVerSourceSemVer.ShouldBe("2.0.0");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [TestCase(true, false, IncrementStrategy.Patch, "2.1.0")]
    [TestCase(false, false, IncrementStrategy.Patch, "2.1.0")]
    [TestCase(true, true, IncrementStrategy.Patch, "2.0.0")]
    [TestCase(false, true, IncrementStrategy.Patch, "2.0.1")]
    [TestCase(true, false, IncrementStrategy.Major, "2.1.0")]
    [TestCase(false, false, IncrementStrategy.Major, "3.0.0")]
    [TestCase(true, true, IncrementStrategy.Major, "2.0.0")]
    [TestCase(false, true, IncrementStrategy.Major, "3.0.0")]
    public void MainToSupportMergePreventionControlsBothIncrements(
        bool preventReceiverIncrement, bool preventMainIncrement, IncrementStrategy receiverIncrement, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithIncrement(IncrementStrategy.Minor)
                .WithPreventIncrementWhenBranchMerged(preventMainIncrement))
            .WithBranch("support", builder => builder.WithIncrement(receiverIncrement)
                .WithPreventIncrementOfMergedBranch(preventReceiverIncrement))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(fullSemVer);
    }

    [Test]
    public void TaggedMainHistoryDoesNotReplayItsCommitMessageBump()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeATaggedCommit("1.0.5");
        fixture.Checkout(MainBranch);
        fixture.MakeACommit("Released API change +semver: major");
        var mainTagSha = fixture.Repository.Head.Tip.Sha;
        fixture.ApplyTag("2.0.0");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);

        var version = fixture.GetVersion(GetConfigurationBuilder().Build());

        version.FullSemVer.ShouldBe("2.0.1");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
    }

    [Test]
    public void DisabledTaggedCommitPreventionRetainsMainBumpOnSupportMerge()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeATaggedCommit("1.0.5");
        fixture.Checkout(MainBranch);
        fixture.MakeACommit("API change +semver: major");
        var mainTagSha = fixture.Repository.Head.Tip.Sha;
        fixture.ApplyTag("2.0.0");
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithPreventIncrementWhenCurrentCommitTagged(false))
            .Build();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe("3.0.0");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("3.0.0");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.SemVerSourceIncrement.ShouldBe("Major");
    }

    [TestCase(CommitMessageIncrementMode.Enabled, "3.0.0")]
    [TestCase(CommitMessageIncrementMode.MergeMessageOnly, "3.0.0")]
    [TestCase(CommitMessageIncrementMode.Disabled, "2.0.0")]
    public void MainToSupportMergeMessageHonorsCommitMessageMode(CommitMessageIncrementMode mode, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.Repository.MergeNoFF(MainBranch, $"Merge branch '{MainBranch}' +semver: major");
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithPreventIncrementWhenBranchMerged(true))
            .WithBranch("support", builder => builder.WithCommitMessageIncrementing(mode))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(fullSemVer);
    }

    [TestCase(CommitMessageIncrementMode.Enabled, "2.1.0")]
    [TestCase(CommitMessageIncrementMode.MergeMessageOnly, "2.0.1")]
    [TestCase(CommitMessageIncrementMode.Disabled, "2.0.1")]
    public void MainCommitMessageModeControlsSupportMergeBump(CommitMessageIncrementMode mode, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.MakeACommit("New API +semver: minor");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithCommitMessageIncrementing(mode))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(fullSemVer);
    }

    [TestCase(IncrementStrategy.None, "2.0.0")]
    [TestCase(IncrementStrategy.Patch, "2.0.1")]
    [TestCase(IncrementStrategy.Minor, "2.1.0")]
    [TestCase(IncrementStrategy.Major, "3.0.0")]
    [TestCase(IncrementStrategy.Inherit, "2.0.1")]
    public void MainBranchIncrementControlsSupportMergeContribution(IncrementStrategy increment, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithIncrement(increment))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe(fullSemVer);
    }

    [Test]
    public void MainBumpResetDoesNotUndoAnEarlierMainlineVersionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.MakeACommit("New API +semver: major");
        fixture.MakeACommit("Reset =semver: none");
        var configuration = GetConfigurationBuilder().Build();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe("3.0.0");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("3.0.0");
        version.SemVerSourceIncrement.ShouldBe("None");
    }

    [Test]
    public void ExplicitMainIncrementKeepsItsOwnCommitMessageMode()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.MakeACommit("New API +semver: minor");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var configuration = GetConfigurationBuilder()
            .WithBranch("support", builder => builder.WithCommitMessageIncrementing(CommitMessageIncrementMode.Disabled))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.1.0");
    }

    [Test]
    public void MainMergeMessageModeIncludesNestedMergeMessages()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.BranchTo("feature/fix");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("feature/fix", "Merge branch 'feature/fix' +semver: minor");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithCommitMessageIncrementing(CommitMessageIncrementMode.MergeMessageOnly))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.1.0");
    }

    [Test]
    public void DeletedMainRefDoesNotChangeSupportMergeVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        var mainTagSha = CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        fixture.Repository.Branches.Remove(MainBranch);

        var version = fixture.GetVersion(GetConfigurationBuilder().Build());

        version.FullSemVer.ShouldBe("2.0.1");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [Test]
    public void TaggedMainToSupportMergeResumesFromItsTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        fixture.ApplyTag("2.0.1");
        var mergeSha = fixture.Repository.Head.Tip.Sha;
        var configuration = GetConfigurationBuilder().Build();

        var taggedVersion = fixture.GetVersion(configuration);
        taggedVersion.FullSemVer.ShouldBe("2.0.1");
        taggedVersion.SemVerSourceSha.ShouldBe(mergeSha);
        taggedVersion.CommitCountSourceDistance.ShouldBe("0");

        fixture.MakeACommit();
        var nextVersion = fixture.GetVersion(configuration);
        nextVersion.FullSemVer.ShouldBe("2.0.2");
        nextVersion.SemVerSourceSha.ShouldBe(mergeSha);
        nextVersion.CommitCountSourceSha.ShouldBe(mergeSha);
        nextVersion.CommitCountSourceDistance.ShouldBe("1");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConfiguredMainAndSupportNamesWorkInBothDirections(bool mergeMainIntoSupport)
    {
        using var fixture = new EmptyRepositoryFixture("trunk");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("maintenance/1.x");
        fixture.MakeATaggedCommit("1.0.5");
        fixture.Checkout("trunk");
        var mainTagSha = fixture.MakeATaggedCommit("2.0.0");
        if (mergeMainIntoSupport)
        {
            fixture.Checkout("maintenance/1.x");
            fixture.MergeNoFF("trunk");
        }
        else
        {
            fixture.MergeNoFF("maintenance/1.x");
        }
        var configuration = GetConfigurationBuilder()
            .WithBranch("main", builder => builder.WithRegularExpression("^trunk$"))
            .WithBranch("support", builder => builder.WithRegularExpression("^maintenance[/-]"))
            .Build();

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("2.0.1");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [Test]
    public void RepeatedMainMergesDoNotReplayEarlierBumps()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.MakeACommit("New API +semver: minor");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var firstMergeSha = fixture.Repository.Head.Tip.Sha;
        var configuration = GetConfigurationBuilder().Build();
        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.1.0");

        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe("2.1.1");
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(firstMergeSha);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [TestCase("feature/fix", "2.0.2-fix.1+1")]
    [TestCase("pull/123", "2.0.2-PullRequest123.1")]
    public void BranchInheritingFromSupportCanTraverseAnEarlierMainMerge(string branchName, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var mainTagSha = CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        var mergeSha = fixture.Repository.Head.Tip.Sha;
        fixture.BranchTo(branchName);
        fixture.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(fullSemVer);
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceSha.ShouldBe(mergeSha);
        version.CommitCountSourceDistance.ShouldBe("1");
    }

    [TestCase("feature/fix", "2.0.2-fix.1+1")]
    [TestCase("pull/123", "2.0.2-PullRequest123.1")]
    public void BranchInheritingFromMainCanTraverseAnEarlierSupportMerge(string branchName, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var mainTagSha = CreateTaggedBranches(fixture);
        fixture.MergeNoFF("support/1.x");
        var mergeSha = fixture.Repository.Head.Tip.Sha;
        fixture.BranchTo(branchName);
        fixture.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();

        var version = fixture.GetVersion(configuration);
        version.FullSemVer.ShouldBe(fullSemVer);
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceSha.ShouldBe(mergeSha);
        version.CommitCountSourceDistance.ShouldBe("1");
    }

    [Test]
    public void HigherSupportTagStillProvidesTheExistingNumericVersionFloor()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/next");
        var supportTagSha = fixture.MakeATaggedCommit("3.0.0");
        fixture.Checkout(MainBranch);
        var mainTagSha = fixture.MakeATaggedCommit("2.0.0");
        fixture.MergeNoFF("support/next");

        var version = fixture.GetVersion(GetConfigurationBuilder().Build());
        version.FullSemVer.ShouldBe("3.0.0");
        version.SemVerSourceSemVer.ShouldBe("3.0.0");
        version.SemVerSourceSha.ShouldBe(supportTagSha);
        version.SemVerSourceIncrement.ShouldBe("None");
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
    }

    [Test]
    public void CustomMainlineMergeRemainsOutsideTheSupportedScope()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("other/mainline");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MakeATaggedCommit("2.0.0");
        fixture.MergeNoFF("other/mainline");
        var configuration = GetConfigurationBuilder()
            .WithBranch("unknown", builder => builder.WithIsMainBranch(true).WithIncrement(IncrementStrategy.Patch))
            .Build();

        Should.Throw<NotImplementedException>(() => fixture.GetVersion(configuration));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void DistinctSupportBranchesRemainOutsideTheSupportedScope(bool mergeIntoMain, bool tagSupportHistory)
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.Checkout("support/1.x");
        fixture.BranchTo("support/other");
        fixture.MakeACommit();
        fixture.Checkout("support/1.x");
        fixture.MakeACommit();
        fixture.MergeNoFF("support/other");

        if (tagSupportHistory)
        {
            fixture.MakeATaggedCommit("1.0.6");
        }

        if (mergeIntoMain)
        {
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("support/1.x");
        }

        Should.Throw<NotImplementedException>(() => fixture.GetVersion(GetConfigurationBuilder().Build()));
    }

    [Test]
    public void SupportMergeValidatesNestedMainHistoryPastTags()
    {
        using var fixture = new EmptyRepositoryFixture();
        CreateTaggedBranches(fixture);
        fixture.BranchTo("other/mainline");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.MergeNoFF("other/mainline");
        fixture.MakeATaggedCommit("2.0.1");
        var configuration = GetConfigurationBuilder()
            .WithBranch("unknown", builder => builder.WithIsMainBranch(true).WithIncrement(IncrementStrategy.Patch))
            .Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.0.1");
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF(MainBranch);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("support/1.x");

        Should.Throw<NotImplementedException>(() => fixture.GetVersion(configuration));
    }

    private static GitFlowConfigurationBuilder GetConfigurationBuilder() => GitFlowConfigurationBuilder.New
        .WithVersionStrategies(VersionStrategies.Mainline)
        .WithDeploymentMode(DeploymentMode.ContinuousDeployment);

    private static string CreateTaggedBranches(EmptyRepositoryFixture fixture)
    {
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeATaggedCommit("1.0.5");
        fixture.Checkout(MainBranch);
        return fixture.MakeATaggedCommit("2.0.0");
    }
}
