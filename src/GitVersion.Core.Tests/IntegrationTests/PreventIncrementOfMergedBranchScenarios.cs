using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using LibGit2Sharp;

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

    [Test]
    public void LatestMergedSourceResetDiscardsEarlierMergedIncrement()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("hotfix", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit("Breaking change +semver: major");
        fixture.MergeTo("main", removeBranchAfterMerging: true);
        fixture.BranchTo("hotfix/foo");
        fixture.MakeACommit("Hotfix =semver: patch");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("1.0.1-4", configuration);
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
    public void UsesInheritedPreventIncrementWhenBranchMergedSetting()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithPreventIncrementWhenBranchMerged(false)
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
                .WithPreventIncrementWhenBranchMerged(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
                .WithPreventIncrementWhenBranchMerged(null)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("1.0.1-3", configuration);
    }

    [Test]
    public void ScoresLocalAndRemoteInheritedSourceBranchesBeforePreferringLocal()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("integration", builder => builder
                .WithRegularExpression("^(origin/)?integration$")
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("main", "develop")
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("integration")
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        var developTip = fixture.MakeACommit();
        fixture.BranchTo("integration");
        fixture.MakeACommit();
        fixture.Repository.Refs.Add(
            "refs/remotes/origin/integration", fixture.Repository.Lookup(developTip).Id);
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.Remove("integration");
        fixture.MakeACommit();
        fixture.BranchTo("integration");
        fixture.MakeACommit();
        fixture.Checkout("feature/foo");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("1.1.0-5", configuration);
    }

    [Test]
    public void HonorsPreventIncrementForTaggedMergedSourceTip()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("hotfix", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementWhenCurrentCommitTagged(true)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("hotfix/foo");
        fixture.MakeATaggedCommit("2.0.0");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("2.0.0-1", configuration);
    }

    [Test]
    public void IgnoresFutureDatedTagWhenEvaluatingMergedSourceMessages()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
                .WithIsMainBranch(true)
                .WithTrackMergeMessage(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit("Breaking change +semver: major");
        var futureSignature = new Signature(
            "A. U. Thor", "thor@valhalla.asgard.com", DateTimeOffset.Now.AddYears(10));
        var futureCommit = fixture.Repository.Commit(
            "Breaking change +semver: major", futureSignature, futureSignature,
            new CommitOptions { AmendPreviousCommit = true });
        fixture.ApplyTag("1.1.0");
        fixture.MergeTo("main", removeBranchAfterMerging: true);
        var targetCommit = fixture.Repository.Head.Tip;
        targetCommit.Parents.Count().ShouldBe(2);
        futureCommit.Committer.When.ShouldBeGreaterThan(targetCommit.Committer.When);

        fixture.AssertFullSemver("2.0.0-2", configuration, commitId: targetCommit.Sha);
    }

    [Test]
    public void RetainsTargetAsPossibleHistoricalSourceBranch()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("main", "develop")
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("1.0.1-3", configuration);
    }

    [Test]
    public void ExcludesIgnoredBranchesFromHistoricalSourceInference()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^develop$"] })
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("main", "develop")
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("1.0.1-3", configuration);
    }

    [Test]
    public void RetainsIgnoredCurrentMainForMergedIncrementResolution()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^main$"] })
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
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("1.1.0-2", configuration);
    }

    [Test]
    public void StopsMergedSourceContributionsAtInterveningTargetTag()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("hotfix", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("2.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit("Breaking change +semver: major");
        fixture.MergeTo("main", removeBranchAfterMerging: true);
        fixture.ApplyTag("1.0.0");
        fixture.BranchTo("hotfix/foo");
        fixture.MakeACommit("Hotfix +semver: patch");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        fixture.AssertFullSemver("2.0.1-4", configuration);
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

    [Test]
    public void ResolvesRetainedInheritedBranchFromHistoricalSource()
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
        fixture.MergeTo("support");
        fixture.Checkout("main");

        fixture.AssertFullSemver("1.1.0-3", configuration);
    }

    [Test]
    public void ResolvesNestedInheritanceFromHistoricalSourceState()
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
            ).WithBranch("integration", builder => builder
                .WithRegularExpression("^integration$")
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("develop", "support")
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("integration")
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.CreateBranch("support");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("integration");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main", removeBranchAfterMerging: true);
        fixture.Checkout("support");
        fixture.MakeACommit();
        fixture.MergeTo("integration");
        fixture.Checkout("main");

        fixture.AssertFullSemver("1.1.0-4", configuration);
    }

    [Test]
    public void ResolvesAllSiblingHistoricalInheritancePaths()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true)
            ).WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("integration-a", builder => builder
                .WithRegularExpression("^integration-a$")
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("develop")
                .WithPreventIncrementWhenBranchMerged(true)
            ).WithBranch("integration-b", builder => builder
                .WithRegularExpression("^integration-b$")
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("develop")
                .WithPreventIncrementWhenBranchMerged(false)
            ).WithBranch("aggregate", builder => builder
                .WithRegularExpression("^aggregate$")
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("integration-a", "integration-b")
            ).WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Inherit)
                .WithSourceBranches("aggregate")
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.CreateBranch("integration-a");
        fixture.CreateBranch("integration-b");
        fixture.BranchTo("aggregate");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.MergeTo("main", removeBranchAfterMerging: true);
        fixture.Checkout("aggregate");
        fixture.MakeACommit();
        fixture.Checkout("main");

        fixture.AssertFullSemver("1.1.0-3", configuration);
    }
}
