using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineIterationTests
{
    [Test]
    public void ChildIsDeeperThanParent()
    {
        var configuration = new BranchConfiguration();
        var parent = new MainlineIteration("id0", new("canonical0"), configuration, null, null, null);
        var child = new MainlineIteration("id1", new("canonical1"), configuration, parent, null, null);

        parent.Depth.ShouldBe(1);
        child.Depth.ShouldBe(2);
    }

    [Test]
    public void SharedHistoryStopsAtBranchPointWithoutCopyingCommits()
    {
        var configuration = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var parent = new MainlineIteration("parent", new("refs/heads/main"), configuration, null, null, null);
        var later = parent.CreateCommit(CreateCommit(), parent.BranchName, configuration);
        var branchPoint = parent.CreateCommit(CreateCommit(), parent.BranchName, configuration);
        var initial = parent.CreateCommit(CreateCommit(), parent.BranchName, configuration);
        var child = new MainlineIteration("child", new("refs/heads/support/1.x"), configuration, parent, branchPoint.Value, later.Value);
        var source = child.CreateCommit(CreateCommit(), child.BranchName, configuration);

        child.Commits.ShouldBe([source]);
        child.GetHistory().ShouldBe([initial, branchPoint, source]);
        child.GetHistory().First().ShouldBeSameAs(initial);
        child.GetHistory().ElementAt(1).ShouldBeSameAs(branchPoint);
        branchPoint.Iteration.ShouldBeSameAs(parent);
        branchPoint.Successor.ShouldBeSameAs(later);
    }

    [Test]
    public void SharedHistoryFollowsNestedBranchPoints()
    {
        var configuration = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var root = new MainlineIteration("root", new("refs/heads/main"), configuration, null, null, null);
        var initial = root.CreateCommit(CreateCommit(), root.BranchName, configuration);
        var first = new MainlineIteration("first", new("refs/heads/support/1.x"), configuration, root, initial.Value, initial.Value);
        var branchPoint = first.CreateCommit(CreateCommit(), first.BranchName, configuration);
        var second = new MainlineIteration("second", new("refs/heads/support/2.x"), configuration, first, branchPoint.Value, branchPoint.Value);

        second.GetHistory().ShouldBe([initial, branchPoint]);
        second.Commits.ShouldBeEmpty();
    }

    [Test]
    public void BranchPointInAnOlderMergedIterationResolvesTheSharedNode()
    {
        var configuration = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var root = new MainlineIteration("root", new("refs/heads/main"), configuration, null, null, null);
        var merge = root.CreateCommit(CreateCommit(), root.BranchName, configuration);
        var oldSource = new MainlineIteration("old", new("refs/heads/feature/old"), configuration, root, null, merge.Value);
        var branchPoint = oldSource.CreateCommit(CreateCommit(), oldSource.BranchName, configuration);
        var initial = oldSource.CreateCommit(CreateCommit(), oldSource.BranchName, configuration);
        merge.AddChildIteration(oldSource);
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), configuration,
            root, branchPoint.Value, merge.Value);

        source.GetBranchedFrom().ShouldBeSameAs(branchPoint);
        source.GetBranchedTo().ShouldBeSameAs(merge);
        source.GetHistory().ShouldBe([initial, branchPoint]);
        source.Commits.ShouldBeEmpty();
        root.Commits.ShouldBe([merge]);
    }

    [Test]
    public void SourceWithOnlySharedHistoryUsesItsOwnTipAndCalculationPosition()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var tip = CreateCommit();
        var receiverTip = CreateCommit();
        var mergeValue = CreateCommit();
        mergeValue.Parents.Returns([receiverTip, tip]);
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null, null);
        var merge = receiver.CreateCommit(mergeValue, receiver.BranchName, branch);
        var later = receiver.CreateCommit(receiverTip, receiver.BranchName, branch);
        later.AddSemanticVersions(new SemanticVersion(3, 0, 0));
        var shared = receiver.CreateCommit(tip, receiver.BranchName, branch);
        shared.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, receiver, shared.Value, merge.Value);
        merge.AddChildIteration(source);
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), mergeValue);

        var version = MainlineVersionCalculator.GetMergedMainlineVersion(source, context);

        source.Commits.ShouldBeEmpty();
        version.GetIncrementedVersion().ToString().ShouldBe("2.0.0-1");
        version.BaseVersionSource.ShouldBeSameAs(tip);
        shared.Successor.ShouldBeSameAs(later);
        merge.IsMergedMainline(configuration).ShouldBeTrue();
    }

    [Test]
    public void EmptySourceInheritsConfigurationFromItsBranchPoint()
    {
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var parentBranch = new BranchConfiguration { Increment = IncrementStrategy.Minor, IsMainBranch = true, Label = "" };
        var parent = new MainlineIteration("parent", new("refs/heads/main"), parentBranch, null, null, null);
        var branchPoint = parent.CreateCommit(CreateCommit(), parent.BranchName, parentBranch);
        var sourceBranch = new BranchConfiguration { Increment = IncrementStrategy.Inherit, IsMainBranch = true, Label = "" };
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), sourceBranch, parent, branchPoint.Value, branchPoint.Value);

        source.GetEffectiveConfiguration(configuration).Increment.ShouldBe(IncrementStrategy.Minor);
        source.Commits.ShouldBeEmpty();
    }

    [Test]
    public void EmptySourceWithoutAncestryRetainsTheReceiverIncrement()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null, null);
        var merge = receiver.CreateCommit(CreateCommit(), receiver.BranchName, branch);
        var initial = receiver.CreateCommit(CreateCommit(), receiver.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        merge.AddChildIteration(new MainlineIteration("empty", new("refs/heads/support/1.x"), branch, receiver, null, merge.Value));

        var version = MainlineVersionCalculator.DetermineBaseVersionRecursive(receiver, "", Substitute.For<IIncrementStrategyFinder>(),
            configuration, new TestEnvironment(), merge.Value);

        merge.IsMergedMainline(configuration).ShouldBeFalse();
        version.GetIncrementedVersion().Patch.ShouldBe(1);
    }

    [Test]
    public void TaggedMergeDoesNotRecalculateItsReleasedSource()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var tip = CreateCommit();
        var receiverTip = CreateCommit();
        var mergeValue = CreateCommit();
        mergeValue.Parents.Returns([receiverTip, tip]);
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null, null);
        var merge = receiver.CreateCommit(mergeValue, receiver.BranchName, branch);
        merge.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, receiver, null, merge.Value);
        source.CreateCommit(tip, source.BranchName, branch).AddSemanticVersions(new SemanticVersion(3, 0, 0));
        merge.AddChildIteration(source);

        var version = MainlineVersionCalculator.DetermineBaseVersionRecursive(receiver, "", Substitute.For<IIncrementStrategyFinder>(),
            configuration, new TestEnvironment(), mergeValue);

        version.GetIncrementedVersion().Major.ShouldBe(2);
        version.BaseVersionSource.ShouldBeSameAs(mergeValue);
    }

    [Test]
    public void VersionCacheDistinguishesRecordedTagHistoriesAtTheSameTip()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var tip = CreateCommit();
        var receiverTip = CreateCommit();
        var mergeValue = CreateCommit();
        mergeValue.Parents.Returns([receiverTip, tip]);
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null, null);
        var merge = receiver.CreateCommit(mergeValue, receiver.BranchName, branch);
        var first = new MainlineIteration("first", new("refs/heads/support/1.x"), branch, receiver, null, merge.Value);
        first.CreateCommit(tip, first.BranchName, branch).AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var second = new MainlineIteration("second", first.BranchName, branch, receiver, null, merge.Value);
        second.CreateCommit(tip, second.BranchName, branch).AddSemanticVersions(new SemanticVersion(3, 0, 0));
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), tip);

        var firstVersion = MainlineVersionCalculator.GetMergedMainlineVersion(first, context);
        var secondVersion = MainlineVersionCalculator.GetMergedMainlineVersion(second, context);

        firstVersion.GetIncrementedVersion().Major.ShouldBe(2);
        secondVersion.GetIncrementedVersion().Major.ShouldBe(3);
        context.MainlineVersions.Count.ShouldBe(2);
        MainlineVersionCalculator.GetMergedMainlineVersion(first, context).ShouldBeSameAs(firstVersion);
        context.MainlineVersions.Count.ShouldBe(2);
    }
    private static ICommit CreateCommit()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        return commit;
    }
}
