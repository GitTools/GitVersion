using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineIterationTests
{
    [TestCase(true, false, "branchedFrom")]
    [TestCase(false, true, "branchedTo")]
    [TestCase(true, true, "branchedFrom")]
    public void ChildIterationRequiresBothBranchReferences(bool missingFrom, bool missingTo, string parameterName)
    {
        var configuration = new BranchConfiguration();
        var parent = new MainlineIteration("parent", new("refs/heads/main"), configuration, null, null, null);

        var exception = Should.Throw<ArgumentException>(() => new MainlineIteration("child", new("refs/heads/source"),
            configuration, parent, missingFrom ? null : CreateCommit(), missingTo ? null : CreateCommit()));

        exception.ParamName.ShouldBe(parameterName);
    }

    [Test]
    public void ChildIsDeeperThanParent()
    {
        var configuration = new BranchConfiguration();
        var parent = new MainlineIteration("id0", new("canonical0"), configuration, null, null, null);
        var child = new MainlineIteration("id1", new("canonical1"), configuration, parent, CreateCommit(), CreateCommit());

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
        MainlineTestCalculation.SelectHistory(parent, child).ShouldBe([initial, branchPoint, source]);
        MainlineTestCalculation.SelectHistory(parent, child).First().ShouldBeSameAs(initial);
        MainlineTestCalculation.SelectHistory(parent, child).ElementAt(1).ShouldBeSameAs(branchPoint);
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
        initial.AddChildIteration(first);
        var branchPoint = first.CreateCommit(CreateCommit(), first.BranchName, configuration);
        var second = new MainlineIteration("second", new("refs/heads/support/2.x"), configuration, first, branchPoint.Value, branchPoint.Value);

        branchPoint.AddChildIteration(second);

        MainlineTestCalculation.SelectHistory(root, second).ShouldBe([initial, branchPoint]);
        second.Commits.ShouldBeEmpty();
    }

    [Test]
    public void BranchPointInAnOlderMergedIterationResolvesTheSharedNode()
    {
        var configuration = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var root = new MainlineIteration("root", new("refs/heads/main"), configuration, null, null, null);
        var merge = root.CreateCommit(CreateCommit(), root.BranchName, configuration);
        var oldSource = new MainlineIteration("old", new("refs/heads/feature/old"), configuration, root, CreateCommit(), merge.Value);
        var branchPoint = oldSource.CreateCommit(CreateCommit(), oldSource.BranchName, configuration);
        var initial = oldSource.CreateCommit(CreateCommit(), oldSource.BranchName, configuration);
        merge.AddChildIteration(oldSource);
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), configuration,
            root, branchPoint.Value, merge.Value);

        new MainlineCommitResolver(root).Resolve(source, source.BranchedFrom).ShouldBeSameAs(branchPoint);
        source.GetBranchedTo().ShouldBeSameAs(merge);
        MainlineTestCalculation.SelectHistory(root, source).ShouldBe([initial, branchPoint]);
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
        var context = MainlineTestCalculation.Context(receiver, configuration, new TestEnvironment(), mergeValue);

        var version = MergedMainlineIncrementer.GetSourceVersion(source, context);

        source.Commits.ShouldBeEmpty();
        version.GetIncrementedVersion().ToString().ShouldBe("2.0.0-1");
        version.BaseVersionSource.ShouldBeSameAs(tip);
        shared.Successor.ShouldBeSameAs(later);
        context.IsMergedMainline(merge).ShouldBeTrue();
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

        new MainlineConfigurationResolver(configuration, new MainlineCommitResolver(parent)).GetEffectiveConfiguration(source).Increment.ShouldBe(IncrementStrategy.Minor);
        source.Commits.ShouldBeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyInheritedSourceResolvesSiblingOrNestedBranchPoint(bool nested)
    {
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var receiverBranch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "stable" };
        var sourceBranch = new BranchConfiguration { Increment = IncrementStrategy.Inherit, IsMainBranch = true };
        var recordedBranch = new BranchConfiguration { Increment = IncrementStrategy.Minor, IsMainBranch = true, Label = "beta" };
        var initialValue = CreateCommit();
        var branchPointValue = CreateCommit();
        branchPointValue.Parents.Returns([initialValue]);
        var olderMergeValue = CreateCommit();
        olderMergeValue.Parents.Returns([initialValue, branchPointValue]);
        var mergeValue = CreateCommit();
        mergeValue.Parents.Returns([olderMergeValue, branchPointValue]);
        var root = new MainlineIteration("root", new("refs/heads/main"), receiverBranch, null, null, null);
        var merge = root.CreateCommit(mergeValue, root.BranchName, receiverBranch);
        var olderMerge = root.CreateCommit(olderMergeValue, root.BranchName, receiverBranch);
        var initial = root.CreateCommit(initialValue, root.BranchName, receiverBranch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var owner = new MainlineIteration("older", new("refs/heads/support/older"), recordedBranch, root, initialValue, olderMergeValue);
        olderMerge.AddChildIteration(owner);
        if (nested)
        {
            var innerMerge = owner.CreateCommit(CreateCommit(), owner.BranchName, recordedBranch);
            var child = new MainlineIteration("nested", new("refs/heads/feature/older"), recordedBranch, owner, initialValue, innerMerge.Value);
            innerMerge.AddChildIteration(child);
            owner = child;
        }
        var branchPoint = owner.CreateCommit(branchPointValue, owner.BranchName, recordedBranch);
        var taggedVersion = new SemanticVersion(2, 0, 0) { PreReleaseTag = new("beta", 1, false) };
        branchPoint.AddSemanticVersions(taggedVersion);
        var source = new MainlineIteration("source", new("refs/heads/support/source"), sourceBranch, root, branchPointValue, mergeValue);
        merge.AddChildIteration(source);

        var context = MainlineTestCalculation.Context(root, configuration, new TestEnvironment(), mergeValue);
        var effective = context.GetEffectiveConfiguration(source);

        effective.Increment.ShouldBe(IncrementStrategy.Minor);
        effective.Label.ShouldBe("beta");
        var version = MergedMainlineIncrementer.GetSourceVersion(source, context);
        version.SemanticVersion.ShouldBe(taggedVersion);
        version.GetIncrementedVersion().ToString().ShouldBe("2.0.0-beta.2");
        version.BaseVersionSource.ShouldBeSameAs(branchPointValue);
        version.GetSemVerSource()!.Commit.ShouldBeSameAs(branchPointValue);
        version.GetSemVerSource()!.Increment.ShouldBe(VersionField.None);
        source.Commits.ShouldBeEmpty();
        branchPoint.Iteration.ShouldBeSameAs(owner);
        root.Commits.ShouldBe([initial, olderMerge, merge]);
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

        var version = MainlineTestCalculation.Calculator(receiver, configuration, new TestEnvironment(), merge.Value)
            .DetermineBaseVersion(receiver, "");

        new MainlineConfigurationResolver(configuration, new MainlineCommitResolver(merge.Iteration)).IsMergedMainline(merge).ShouldBeFalse();
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
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, receiver, receiverTip, merge.Value);
        source.CreateCommit(tip, source.BranchName, branch).AddSemanticVersions(new SemanticVersion(3, 0, 0));
        merge.AddChildIteration(source);

        var version = MainlineTestCalculation.Calculator(receiver, configuration, new TestEnvironment(), mergeValue)
            .DetermineBaseVersion(receiver, "");

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
        var first = new MainlineIteration("first", new("refs/heads/support/1.x"), branch, receiver, receiverTip, merge.Value);
        first.CreateCommit(tip, first.BranchName, branch).AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var second = new MainlineIteration("second", first.BranchName, branch, receiver, receiverTip, merge.Value);
        second.CreateCommit(tip, second.BranchName, branch).AddSemanticVersions(new SemanticVersion(3, 0, 0));
        var context = MainlineTestCalculation.Context(receiver, configuration, new TestEnvironment(), tip);

        var firstVersion = MergedMainlineIncrementer.GetSourceVersion(first, context);
        var secondVersion = MergedMainlineIncrementer.GetSourceVersion(second, context);

        firstVersion.GetIncrementedVersion().Major.ShouldBe(2);
        secondVersion.GetIncrementedVersion().Major.ShouldBe(3);
        context.MainlineVersions.Count.ShouldBe(2);
        MergedMainlineIncrementer.GetSourceVersion(first, context).ShouldBeSameAs(firstVersion);
        context.MainlineVersions.Count.ShouldBe(2);
    }
    [Test]
    public void MainlineMergeAtInheritedBranchBoundaryPreservesBothOperators()
    {
        var mainline = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var inherited = new BranchConfiguration { Increment = IncrementStrategy.Inherit, IsMainBranch = false, Label = "feature" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var initialValue = CreateCommit();
        var sourceValue = CreateCommit();
        sourceValue.Parents.Returns([initialValue]);
        var mergeValue = CreateCommit();
        mergeValue.Parents.Returns([initialValue, sourceValue]);
        var root = new MainlineIteration("feature", new("refs/heads/feature/current"), inherited, null, null, null);
        var mainBranch = new ReferenceName("refs/heads/main");
        var merge = root.CreateCommit(mergeValue, mainBranch, mainline);
        var initial = root.CreateCommit(initialValue, mainBranch, mainline);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var source = new MainlineIteration("source", new("refs/heads/support/source"), mainline,
            root, initialValue, mergeValue);
        source.CreateCommit(sourceValue, source.BranchName, mainline).AddSemanticVersions(new SemanticVersion(2, 0, 0));
        merge.AddChildIteration(source);
        var calculator = MainlineTestCalculation.Calculator(root, configuration, new TestEnvironment(), mergeValue);

        var version = calculator.DetermineBaseVersion(root, "");

        version.GetIncrementedVersion().ToString().ShouldBe("2.0.1-feature.1");
        version.BaseVersionSource.ShouldBeSameAs(mergeValue);
        version.GetSemVerSource().Commit.ShouldBeSameAs(sourceValue);
        version.GetSemVerSource().Increment.ShouldBe(VersionField.Patch);
    }

    private static ICommit CreateCommit()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        return commit;
    }
}
