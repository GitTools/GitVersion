using GitVersion.Configuration;
using GitVersion.Testing.Extensions;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PreventIncrementOfMergedBranchScenarios
{
    [TestCase(false, false, "1.0.1-2")]
    [TestCase(false, true, "1.0.1-2")]
    [TestCase(false, null, "1.1.0-2")]
    [TestCase(true, false, "1.1.0-2")]
    [TestCase(true, true, "1.0.1-2")]
    [TestCase(true, null, "1.1.0-2")]
    public void SelectsIncrementFromTargetAndMergedBranchConfiguration(
        bool preventIncrementOfMergedBranch,
        bool? preventIncrementWhenBranchMerged,
        string expectedVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithPreventIncrementWhenBranchMerged(null)
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(preventIncrementOfMergedBranch)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
                .WithPreventIncrementWhenBranchMerged(preventIncrementWhenBranchMerged)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");

        fixture.AssertFullSemver(expectedVersion, configuration);
    }

    [Test]
    public void UsesHotfixIncrementWhenHotfixIsMergedIntoMain()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("hotfix", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("hotfix/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");

        fixture.AssertFullSemver("1.0.1-2", configuration);
    }

    [Test]
    public void UsesFeatureIncrementWhenFeatureIsMergedIntoMain()
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");

        fixture.AssertFullSemver("1.1.0-2", configuration);
    }

    [Test]
    public void RetainsMergedBranchIncrementAfterSubsequentTargetCommit()
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.1.0-3", configuration);
    }

    [Test]
    public void UsesHighestIncrementFromMultipleMergedBranches()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("hotfix", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("hotfix/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");

        fixture.AssertFullSemver("1.1.0-4", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IncludesTargetIncrementForTargetCommit(bool commitAfterMerge)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("hotfix", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        if (!commitAfterMerge)
        {
            fixture.MakeACommit();
        }
        fixture.BranchTo("hotfix/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");
        if (commitAfterMerge)
        {
            fixture.MakeACommit();
        }

        fixture.AssertFullSemver("1.1.0-3", configuration);
    }

    [Test]
    public void IgnoresSourceCommitMessageWhenSourceIncrementIsPrevented()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
                .WithPreventIncrementWhenBranchMerged(true)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit("Breaking change +semver: major");
        fixture.MergeTo("main");

        fixture.AssertFullSemver("1.0.1-2", configuration);
    }

    [TestCase("Feature +semver: minor", "1.1.0-2")]
    [TestCase("Feature =semver: patch", "1.0.1-2")]
    public void UsesEffectiveSourceCommitMessageIncrement(string message, string expectedVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Major)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit(message);
        fixture.MergeTo("main");

        fixture.AssertFullSemver(expectedVersion, configuration);
    }

    [Test]
    public void ResolvesInheritedIncrementFromMergedBranchSource()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("1.1.0-3", configuration);
    }

    [Test]
    public void ResolvesInheritedIncrementFromHistoricalMergedBranchTip()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main", removeBranchAfterMerging: true);
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.Checkout("main");

        fixture.AssertFullSemver("1.1.0-3", configuration);
    }

    [Test]
    public void PrefersHistoricalSourceOverLaterAbsorbingBranch()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("support", builder => builder
                .WithRegularExpression("^support$")
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("develop", "support")
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.CreateBranch("support");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main");
        fixture.Checkout("feature/foo");
        fixture.MergeTo("support", removeBranchAfterMerging: true);
        fixture.Checkout("main");

        fixture.AssertFullSemver("1.1.0-3", configuration);
    }
}
