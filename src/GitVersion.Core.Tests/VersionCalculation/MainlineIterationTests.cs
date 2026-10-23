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
        var parent = new MainlineIteration("id0", new("canonical0"), configuration, null, null);
        var child = new MainlineIteration("id1", new("canonical1"), configuration, parent, null);

        parent.Depth.ShouldBe(1);
        child.Depth.ShouldBe(2);
    }

    [Test]
    public void SharedHistoryStopsAtBranchPointWithoutCopyingCommits()
    {
        var configuration = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var parent = new MainlineIteration("parent", new("refs/heads/main"), configuration, null, null);
        var later = parent.CreateCommit(Substitute.For<ICommit>(), parent.BranchName, configuration);
        var branchPoint = parent.CreateCommit(Substitute.For<ICommit>(), parent.BranchName, configuration);
        var initial = parent.CreateCommit(Substitute.For<ICommit>(), parent.BranchName, configuration);
        var child = new MainlineIteration("child", new("refs/heads/support/1.x"), configuration, parent, later)
        {
            BranchedFrom = branchPoint
        };
        var source = child.CreateCommit(Substitute.For<ICommit>(), child.BranchName, configuration);

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
        var root = new MainlineIteration("root", new("refs/heads/main"), configuration, null, null);
        var initial = root.CreateCommit(Substitute.For<ICommit>(), root.BranchName, configuration);
        var first = new MainlineIteration("first", new("refs/heads/support/1.x"), configuration, root, initial)
        {
            BranchedFrom = initial
        };
        var branchPoint = first.CreateCommit(Substitute.For<ICommit>(), first.BranchName, configuration);
        var second = new MainlineIteration("second", new("refs/heads/support/2.x"), configuration, first, branchPoint)
        {
            BranchedFrom = branchPoint
        };

        second.GetHistory().ShouldBe([initial, branchPoint]);
        second.Commits.ShouldBeEmpty();
    }

    [Test]
    public void SourceWithOnlySharedHistoryUsesItsOwnTipAndCalculationPosition()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var tip = GitRepositoryTestingExtensions.CreateMockCommit();
        tip.Equals(tip).Returns(true);
        var receiverTip = GitRepositoryTestingExtensions.CreateMockCommit();
        var mergeValue = GitRepositoryTestingExtensions.CreateMockCommit();
        mergeValue.Parents.Returns([receiverTip, tip]);
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null);
        var merge = receiver.CreateCommit(mergeValue, receiver.BranchName, branch);
        var later = receiver.CreateCommit(receiverTip, receiver.BranchName, branch);
        later.AddSemanticVersions(new SemanticVersion(3, 0, 0));
        var shared = receiver.CreateCommit(tip, receiver.BranchName, branch);
        shared.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, receiver, merge)
        {
            BranchedFrom = shared
        };
        merge.AddChildIteration(source);
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), mergeValue);

        var version = MainlineVersionStrategy.GetMergedMainlineVersion(source, context);

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
        var parent = new MainlineIteration("parent", new("refs/heads/main"), parentBranch, null, null);
        var branchPoint = parent.CreateCommit(GitRepositoryTestingExtensions.CreateMockCommit(), parent.BranchName, parentBranch);
        var sourceBranch = new BranchConfiguration { Increment = IncrementStrategy.Inherit, IsMainBranch = true, Label = "" };
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), sourceBranch, parent, branchPoint)
        {
            BranchedFrom = branchPoint
        };

        source.GetEffectiveConfiguration(configuration).Increment.ShouldBe(IncrementStrategy.Minor);
        source.Commits.ShouldBeEmpty();
    }

    [Test]
    public void EmptySourceWithoutAncestryRetainsTheReceiverIncrement()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null);
        var merge = receiver.CreateCommit(GitRepositoryTestingExtensions.CreateMockCommit(), receiver.BranchName, branch);
        var initial = receiver.CreateCommit(GitRepositoryTestingExtensions.CreateMockCommit(), receiver.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        merge.AddChildIteration(new MainlineIteration("empty", new("refs/heads/support/1.x"), branch, receiver, merge));

        var version = MainlineVersionStrategy.DetermineBaseVersionRecursive(receiver, "", Substitute.For<IIncrementStrategyFinder>(),
            configuration, new TestEnvironment(), merge.Value);

        merge.IsMergedMainline(configuration).ShouldBeFalse();
        version.GetIncrementedVersion().Patch.ShouldBe(1);
    }

    [Test]
    public void TaggedMergeDoesNotRecalculateItsReleasedSource()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var tip = GitRepositoryTestingExtensions.CreateMockCommit();
        var receiverTip = GitRepositoryTestingExtensions.CreateMockCommit();
        var mergeValue = GitRepositoryTestingExtensions.CreateMockCommit();
        mergeValue.Parents.Returns([receiverTip, tip]);
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null);
        var merge = receiver.CreateCommit(mergeValue, receiver.BranchName, branch);
        merge.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, receiver, merge);
        source.CreateCommit(tip, source.BranchName, branch).AddSemanticVersions(new SemanticVersion(3, 0, 0));
        merge.AddChildIteration(source);

        var version = MainlineVersionStrategy.DetermineBaseVersionRecursive(receiver, "", Substitute.For<IIncrementStrategyFinder>(),
            configuration, new TestEnvironment(), mergeValue);

        version.GetIncrementedVersion().Major.ShouldBe(2);
        version.BaseVersionSource.ShouldBeSameAs(mergeValue);
    }

    [Test]
    public void VersionCacheDistinguishesRecordedTagHistoriesAtTheSameTip()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var tip = GitRepositoryTestingExtensions.CreateMockCommit();
        var receiverTip = GitRepositoryTestingExtensions.CreateMockCommit();
        var mergeValue = GitRepositoryTestingExtensions.CreateMockCommit();
        mergeValue.Parents.Returns([receiverTip, tip]);
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null);
        var merge = receiver.CreateCommit(mergeValue, receiver.BranchName, branch);
        var first = new MainlineIteration("first", new("refs/heads/support/1.x"), branch, receiver, merge);
        first.CreateCommit(tip, first.BranchName, branch).AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var second = new MainlineIteration("second", first.BranchName, branch, receiver, merge);
        second.CreateCommit(tip, second.BranchName, branch).AddSemanticVersions(new SemanticVersion(3, 0, 0));
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), tip);

        var firstVersion = MainlineVersionStrategy.GetMergedMainlineVersion(first, context);
        var secondVersion = MainlineVersionStrategy.GetMergedMainlineVersion(second, context);

        firstVersion.GetIncrementedVersion().Major.ShouldBe(2);
        secondVersion.GetIncrementedVersion().Major.ShouldBe(3);
        context.MainlineVersions.Count.ShouldBe(2);
        MainlineVersionStrategy.GetMergedMainlineVersion(first, context).ShouldBeSameAs(firstVersion);
        context.MainlineVersions.Count.ShouldBe(2);
    }
}
