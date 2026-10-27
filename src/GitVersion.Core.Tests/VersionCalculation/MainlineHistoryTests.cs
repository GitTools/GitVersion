using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineHistoryTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ExplicitEmptyLabelOverridesTheRecordedBranchLabel(bool mainline)
    {
        var initialValue = Commit();
        var releaseValue = Commit(initialValue);
        var tipValue = Commit(releaseValue);
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = mainline, Label = "beta" };
        var iteration = new MainlineIteration("root", new("refs/heads/source"), branch, null, null, null);
        var tip = iteration.CreateCommit(tipValue, iteration.BranchName, branch);
        var release = iteration.CreateCommit(releaseValue, iteration.BranchName, branch);
        release.AddSemanticVersions(new SemanticVersion(2, 0, 0) { PreReleaseTag = new("beta", 1, false) });
        var initial = iteration.CreateCommit(initialValue, iteration.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var history = History(tipValue);

        var explicitLabel = history.Select(iteration, "", false);
        var recordedLabel = history.Select(iteration, null, false);

        explicitLabel.Commits.ShouldBe([initial, release, tip]);
        explicitLabel.HasBoundary.ShouldBeTrue();
        recordedLabel.Commits.ShouldBe([release, tip]);
        recordedLabel.HasBoundary.ShouldBeTrue();
        iteration.Commits.ShouldBe([initial, release, tip]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RepeatedIgnoredAncestrySelectionReusesTraversal(bool retainAncestor)
    {
        var cutoff = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var initialValue = Commit();
        initialValue.When.Returns(cutoff.AddDays(retainAncestor ? 1 : -2));
        var ignoredTipValue = Commit(initialValue);
        ignoredTipValue.When.Returns(cutoff.AddDays(-1));
        var branch = Branch(true);
        var iteration = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var ignoredTip = iteration.CreateCommit(ignoredTipValue, iteration.BranchName, branch);
        var initial = iteration.CreateCommit(initialValue, iteration.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(new IgnoreConfiguration { Before = cutoff }).Build();
        var history = new MainlineHistory(configuration, new TestEnvironment(), ignoredTipValue);

        var first = history.Select(iteration, "", false);
        first.Commits.ShouldBe(retainAncestor ? [initial] : []);
        first.HasBoundary.ShouldBe(retainAncestor);
        first.MissingCommit.ShouldBeNull();
        ignoredTipValue.ClearReceivedCalls();

        var repeated = history.Select(iteration, "", false);

        repeated.Commits.ShouldBe(retainAncestor ? [initial] : []);
        repeated.HasBoundary.ShouldBe(retainAncestor);
        repeated.MissingCommit.ShouldBeNull();
        _ = ignoredTipValue.DidNotReceive().Parents;
        _ = ignoredTipValue.DidNotReceive().When;
        iteration.Commits.ShouldBe([initial, ignoredTip]);
    }

    [Test]
    public void SharedIgnoredParentIsTraversedOncePerHistorySelection()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var initialValue = Commit();
        initialValue.When.Returns(start);
        var ignoredValue = Commit(initialValue);
        ignoredValue.When.Returns(start.AddDays(1));
        var leftValue = Commit(ignoredValue);
        leftValue.When.Returns(start.AddDays(2));
        var rightValue = Commit(ignoredValue);
        rightValue.When.Returns(start.AddDays(3));
        var mergeValue = Commit(leftValue);
        mergeValue.Parents.Returns([leftValue, rightValue]);
        mergeValue.When.Returns(start.AddDays(4));
        var branch = Branch(true);
        var iteration = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = iteration.CreateCommit(mergeValue, iteration.BranchName, branch);
        var right = iteration.CreateCommit(rightValue, iteration.BranchName, branch);
        var left = iteration.CreateCommit(leftValue, iteration.BranchName, branch);
        var ignored = iteration.CreateCommit(ignoredValue, iteration.BranchName, branch);
        var initial = iteration.CreateCommit(initialValue, iteration.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [ignoredValue.Sha] }).Build();
        ignoredValue.ClearReceivedCalls();

        var selected = new MainlineHistory(configuration, new TestEnvironment(), mergeValue).Select(iteration, "", false);

        selected.Commits.ShouldBe([initial, left, right, merge]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        _ = ignoredValue.Received(1).Parents;
        iteration.Commits.ShouldBe([initial, ignored, left, right, merge]);
    }

    [Test]
    public void IgnoredAncestryResultsDoNotLeakBetweenHistoryInstances()
    {
        var initialValue = Commit();
        var tipValue = Commit(initialValue);
        var branch = Branch(true);
        var iteration = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var tip = iteration.CreateCommit(tipValue, iteration.BranchName, branch);
        var initial = iteration.CreateCommit(initialValue, iteration.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [tipValue.Sha] }).Build();
        var filteredHistory = new MainlineHistory(configuration, new TestEnvironment(), tipValue);
        var unfilteredHistory = History(tipValue);

        filteredHistory.Select(iteration, "", false).Commits.ShouldBe([initial]);
        unfilteredHistory.Select(iteration, "", false).Commits.ShouldBe([initial, tip]);
        filteredHistory.Select(iteration, "", false).Commits.ShouldBe([initial]);
        iteration.Commits.ShouldBe([initial, tip]);
    }

    [TestCase(false, "beta")]
    [TestCase(true, "beta")]
    [TestCase(false, null)]
    [TestCase(true, null)]
    public void InheritedReleaseStopsAtFirstExplicitIncrement(bool mainline, string? label)
    {
        var explicitBranch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = mainline, Label = "beta" };
        var inheritedBranch = new BranchConfiguration { Increment = IncrementStrategy.Inherit, IsMainBranch = mainline, Label = label };
        var initialValue = Commit();
        var boundaryValue = Commit(initialValue);
        var releaseValue = Commit(boundaryValue);
        var tipValue = Commit(releaseValue);
        var iteration = new MainlineIteration("root", new("refs/heads/source"), inheritedBranch, null, null, null);
        var tip = iteration.CreateCommit(tipValue, iteration.BranchName, inheritedBranch);
        var release = iteration.CreateCommit(releaseValue, iteration.BranchName, inheritedBranch);
        release.AddSemanticVersions(new SemanticVersion(2, 0, 0) { PreReleaseTag = new(label ?? "source", 1, false) });
        var boundary = iteration.CreateCommit(boundaryValue, iteration.BranchName, explicitBranch);
        var initial = iteration.CreateCommit(initialValue, iteration.BranchName, explicitBranch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0) { PreReleaseTag = new("beta", 1, false) });
        var configuration = GitFlowConfigurationBuilder.New.WithLabel("source").Build();

        var selected = new MainlineHistory(configuration, new TestEnvironment(), tipValue).Select(iteration, null, false);

        selected.Commits.ShouldBe([boundary, release, tip]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        var configurations = new MainlineConfigurationResolver(configuration, new MainlineBranchPointResolver(release.Iteration));
        configurations.GetEffectiveConfiguration(release).Increment.ShouldBe(IncrementStrategy.Patch);
        configurations.GetEffectiveConfiguration(release).Label.ShouldBe("beta");
        iteration.Commits.ShouldBe([initial, boundary, release, tip]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IgnoredMergeRetainsSourceHistory(bool mainline)
    {
        var branch = Branch(mainline);
        var initialValue = Commit();
        var sourceValue = Commit(initialValue);
        var mergeValue = Commit(initialValue);
        mergeValue.Parents.Returns([initialValue, sourceValue]);
        var tipValue = Commit(mergeValue);
        var root = new MainlineIteration("root", new("refs/heads/feature/receiver"), branch, null, null, null);
        var tip = root.CreateCommit(tipValue, root.BranchName, branch);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        var initial = root.CreateCommit(initialValue, root.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var child = new MainlineIteration("child", new("refs/heads/feature/source"), branch, root, initialValue, mergeValue);
        var source = child.CreateCommit(sourceValue, child.BranchName, branch);
        merge.AddChildIteration(child);
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(
            new IgnoreConfiguration { Shas = [mergeValue.Sha] }).Build();

        var selected = new MainlineHistory(configuration, new TestEnvironment(), tipValue).Select(root, "", false);

        selected.Commits.ShouldBe([initial, source, tip]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        root.Commits.ShouldBe([initial, merge, tip]);
        child.Commits.ShouldBe([source]);
        source.Iteration.ShouldBeSameAs(child);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IgnoredBranchPointStillConnectsToUnignoredAncestry(bool mainline)
    {
        var branch = Branch(true);
        var initialValue = Commit();
        var ignoredValue = Commit(initialValue);
        var sourceValue = Commit(ignoredValue);
        var mergeValue = Commit(ignoredValue);
        mergeValue.Parents.Returns([ignoredValue, sourceValue]);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        var ignored = root.CreateCommit(ignoredValue, root.BranchName, branch);
        var initial = root.CreateCommit(initialValue, root.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var sourceBranch = Branch(mainline);
        var child = new MainlineIteration("child", new("refs/heads/source"), sourceBranch, root, ignoredValue, mergeValue);
        var source = child.CreateCommit(sourceValue, child.BranchName, sourceBranch);
        merge.AddChildIteration(child);
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(
            new IgnoreConfiguration { Shas = [ignoredValue.Sha] }).Build();
        var resolver = new MainlineBranchPointResolver(root);

        var selected = new MainlineHistory(configuration, new TestEnvironment(), mergeValue, resolver).Select(child, "", true);

        selected.Commits.ShouldBe([initial, source]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        resolver.Resolve(child).ShouldBeSameAs(ignored);
        root.Commits.ShouldBe([initial, ignored, merge]);
        child.Commits.ShouldBe([source]);
        child.BranchedFrom.ShouldBeSameAs(ignoredValue);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IgnoredCommitsAreExcludedWithoutChangingRecordedHistory(bool mainline)
    {
        var branch = Branch(mainline);
        var initialValue = Commit();
        var ignoredReleaseValue = Commit(initialValue);
        var retainedValue = Commit(ignoredReleaseValue);
        var ignoredTipValue = Commit(retainedValue);
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(
            new IgnoreConfiguration { Shas = [ignoredReleaseValue.Sha, ignoredTipValue.Sha] }).Build();
        var iteration = new MainlineIteration("root", new("refs/heads/source"), branch, null, null, null);
        var ignoredTip = iteration.CreateCommit(ignoredTipValue, iteration.BranchName, branch);
        var ignoredTransition = iteration.CreateCommit(null, iteration.BranchName, branch);
        var retained = iteration.CreateCommit(retainedValue, iteration.BranchName, branch);
        var ignoredRelease = iteration.CreateCommit(ignoredReleaseValue, iteration.BranchName, branch);
        ignoredRelease.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var initial = iteration.CreateCommit(initialValue, iteration.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));

        var selected = new MainlineHistory(configuration, new TestEnvironment(), ignoredTipValue).Select(iteration, "", false);

        selected.Commits.ShouldBe([initial, retained]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        iteration.Commits.ShouldBe([initial, ignoredRelease, retained, ignoredTransition, ignoredTip]);
        retained.Predecessor.ShouldBeSameAs(ignoredRelease);
        ignoredTransition.Predecessor.ShouldBeSameAs(retained);
        ignoredTransition.Successor.ShouldBeSameAs(ignoredTip);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EntirelyIgnoredFeatureHasNoCalculationHistory(bool includeSharedHistory)
    {
        var branch = Branch(true);
        var initialValue = Commit();
        var sourceValue = Commit(initialValue);
        var mergeValue = Commit(initialValue);
        mergeValue.Parents.Returns([initialValue, sourceValue]);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        var initial = root.CreateCommit(initialValue, root.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var feature = new MainlineIteration("feature", new("refs/heads/feature/ignored"), Branch(false), root, initialValue, mergeValue);
        var source = feature.CreateCommit(sourceValue, feature.BranchName, feature.Configuration);
        merge.AddChildIteration(feature);
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(
            new IgnoreConfiguration { Shas = [sourceValue.Sha] }).Build();
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), mergeValue)
        {
            BranchPointResolver = new MainlineBranchPointResolver(root),
            IncludeSharedHistory = includeSharedHistory
        };

        context.HasChildHistory(merge).ShouldBeFalse();
        feature.Commits.ShouldBe([source]);
        feature.BranchedFrom.ShouldBeSameAs(initialValue);
        feature.ParentIteration.ShouldBeSameAs(root);
    }

    [Test]
    public void SharedHistoryModeSurvivesRecursiveAncestrySelection()
    {
        var a = Commit();
        var sharedValue = Commit(a);
        var branchPointValue = Commit(sharedValue);
        var sourceValue = Commit(branchPointValue);
        var mergeValue = Commit(a);
        mergeValue.Parents.Returns([a, branchPointValue]);
        var branch = Branch(true);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        var shared = root.CreateCommit(sharedValue, root.BranchName, branch);
        var initial = root.CreateCommit(a, root.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var older = new MainlineIteration("older", new("refs/heads/feature/older"), Branch(false), root, a, mergeValue);
        var branchPoint = older.CreateCommit(branchPointValue, older.BranchName, older.Configuration);
        merge.AddChildIteration(older);
        var source = new MainlineIteration("source", new("refs/heads/source"), Branch(false), null, branchPointValue, null);
        var sourceCommit = source.CreateCommit(sourceValue, source.BranchName, source.Configuration);
        var history = new MainlineHistory(GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(), sourceValue,
            new MainlineBranchPointResolver(root));

        history.Select(source, "", true).Commits.ShouldBe([initial, branchPoint, sourceCommit]);
        var selected = history.Select(source, "", true, includeSharedHistory: true);

        selected.Commits.ShouldBe([initial, shared, branchPoint, sourceCommit]);
        selected.Commits[1].ShouldBeSameAs(shared);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        source.Commits.ShouldBe([sourceCommit]);
        older.Commits.ShouldBe([branchPoint]);
        root.Commits.ShouldBe([initial, shared, merge]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InheritedMainlineWithoutMatchingReleaseDoesNotDuplicateAncestry(bool addUnmatchedPrerelease)
    {
        var a = Commit();
        var sharedValue = Commit(a);
        var branchPointValue = Commit(sharedValue);
        var olderMergeValue = Commit(sharedValue);
        olderMergeValue.Parents.Returns([sharedValue, branchPointValue]);
        var receiverValue = Commit(olderMergeValue);
        var sourceValue = Commit(branchPointValue);
        var mergeValue = Commit(receiverValue);
        mergeValue.Parents.Returns([receiverValue, sourceValue]);
        var branch = Branch(true);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        var receiver = root.CreateCommit(receiverValue, root.BranchName, branch);
        var olderMerge = root.CreateCommit(olderMergeValue, root.BranchName, branch);
        var shared = root.CreateCommit(sharedValue, root.BranchName, branch);
        var initial = root.CreateCommit(a, root.BranchName, branch);
        if (addUnmatchedPrerelease)
        {
            initial.AddSemanticVersions(new SemanticVersion(1, 0, 0) { PreReleaseTag = new("alpha", 1, false) });
        }
        var older = new MainlineIteration("older", new("refs/heads/feature/older"), Branch(false), root, a, olderMergeValue);
        var branchPoint = older.CreateCommit(branchPointValue, older.BranchName, older.Configuration);
        olderMerge.AddChildIteration(older);
        var sourceBranch = new BranchConfiguration { Increment = IncrementStrategy.Inherit, Label = "beta" };
        var source = new MainlineIteration("source", new("refs/heads/source"), sourceBranch, root, branchPointValue, mergeValue);
        var sourceCommit = source.CreateCommit(sourceValue, source.BranchName, sourceBranch);
        merge.AddChildIteration(source);
        var configuration = GitFlowConfigurationBuilder.New.Build();
        new MainlineConfigurationResolver(configuration, new MainlineBranchPointResolver(source)).GetEffectiveConfiguration(source).IsMainBranch.ShouldBeTrue();

        var selected = new MainlineHistory(configuration, new TestEnvironment(), mergeValue)
            .Select(source, "beta", true, includeSharedHistory: true);

        selected.Commits.ShouldBe([initial, shared, branchPoint, sourceCommit]);
        selected.HasBoundary.ShouldBeFalse();
        selected.MissingCommit.ShouldBeNull();
        source.Commits.ShouldBe([sourceCommit]);
        older.Commits.ShouldBe([branchPoint]);
        root.Commits.ShouldBe([initial, shared, olderMerge, receiver, merge]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UntrackedMergeRetainsBothParentsEvenWhenTheMergeIsIgnored(bool ignoreMerge)
    {
        var branch = Branch(true);
        var a = Commit();
        var b = Commit(a);
        var c = Commit(a);
        var merge = Commit(b);
        merge.Parents.Returns([b, c]);
        var tip = Commit(merge);
        var values = new[] { a, b, c, merge, tip };
        for (var index = 0; index < values.Length; index++)
        {
            values[index].When.Returns(DateTimeOffset.UnixEpoch.AddSeconds(index));
        }
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(
            IgnoreConfigurationBuilder.New.WithShas(ignoreMerge ? new[] { merge.Sha } : Array.Empty<string>()).Build()).Build();
        var iteration = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var nodes = values.Where(value => !ignoreMerge || !value.Equals(merge)).Reverse()
            .Select(value => iteration.CreateCommit(value, iteration.BranchName, branch)).Reverse().ToArray();
        nodes[0].AddSemanticVersions(new SemanticVersion(1, 0, 0));

        var selected = new MainlineHistory(configuration, new TestEnvironment(), tip).Select(iteration, "", false);

        selected.Commits.ShouldBe(nodes);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
    }

    [Test]
    public void IndependentSourceCanCalculateSharedFeatureHistoryWithoutCopyingIt()
    {
        var branch = Branch(true);
        var a = Commit();
        var featureValue = Commit(a);
        var sourceValue = Commit(a);
        var featureMerge = Commit(sourceValue);
        featureMerge.Parents.Returns([sourceValue, featureValue]);
        var mergeValue = Commit(a);
        mergeValue.Parents.Returns([a, featureMerge]);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        var shared = root.CreateCommit(featureValue, root.BranchName, branch);
        root.CreateCommit(a, root.BranchName, branch);
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, root, a, mergeValue);
        merge.AddChildIteration(source);
        var nestedMerge = source.CreateCommit(featureMerge, source.BranchName, branch);
        source.CreateCommit(sourceValue, source.BranchName, branch);
        var feature = new MainlineIteration("feature", new("refs/heads/feature/shared"), Branch(false), source, a, featureMerge);
        nestedMerge.AddChildIteration(feature);
        var configuration = GitFlowConfigurationBuilder.New.Build();
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), mergeValue)
        {
            BranchPointResolver = new MainlineBranchPointResolver(root)
        };

        context.HasChildHistory(nestedMerge).ShouldBeFalse();
        (context with { IncludeSharedHistory = true }).HasChildHistory(nestedMerge).ShouldBeTrue();
        var selected = new MainlineHistory(configuration, context.Environment, mergeValue, context.BranchPointResolver)
            .Select(feature, "", false, includeSharedHistory: true);
        selected.Commits.ShouldBe([shared]);
        selected.Commits[0].ShouldBeSameAs(shared);
        feature.Commits.ShouldBeEmpty();
        shared.Iteration.ShouldBeSameAs(root);
    }

    [Test]
    public void MainlineSelectionFollowsFirstParentsWithoutReplayingSideHistory()
    {
        var branch = Branch(true);
        var a = Commit();
        var b = Commit(a);
        var receiverTip = Commit(a);
        var sourceTip = Commit(b);
        var mergeValue = Commit(receiverTip);
        mergeValue.Parents.Returns([receiverTip, sourceTip]);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        var receiver = root.CreateCommit(receiverTip, root.BranchName, branch);
        var shared = root.CreateCommit(b, root.BranchName, branch);
        var initial = root.CreateCommit(a, root.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, root, a, mergeValue);
        var sourceCommit = source.CreateCommit(sourceTip, source.BranchName, branch);
        merge.AddChildIteration(source);
        var history = History(mergeValue);

        history.Select(root, "", false).Commits.ShouldBe([initial, receiver, merge]);
        var independent = history.Select(source, "", true);
        independent.Commits.ShouldBe([initial, shared, sourceCommit]);
        independent.Commits[1].ShouldBeSameAs(shared);
        root.Commits.ShouldBe([initial, shared, receiver, merge]);
        source.Commits.ShouldBe([sourceCommit]);
        receiver.Predecessor.ShouldBeSameAs(shared);
    }

    [Test]
    public void ChildContextKeepsCacheButResetsCalculationState()
    {
        var commit = Commit();
        var branch = Branch(true);
        var iteration = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var predecessor = iteration.CreateCommit(commit, iteration.BranchName, branch);
        var version = new SemanticVersion(3, 0, 0);
        var parent = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(),
            GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(), commit)
        {
            TargetLabel = "beta",
            BranchPointResolver = new MainlineBranchPointResolver(iteration),
            IncludeSharedHistory = true,
            SemanticVersion = version,
            SemVerSource = new SemanticVersionSource("tag", version, commit, VersionField.None),
            Increment = VersionField.Major,
            CommitIncrement = VersionField.Minor,
            BaseVersionSource = commit,
            Predecessor = predecessor,
            HasSuccessor = true,
            Label = "previous",
            ForceIncrement = true,
            SuppressBranchIncrement = true
        };
        var cached = new BaseVersion("cached", version);
        parent.MainlineVersions.Add(iteration, cached);
        parent.AddAlternativeSemanticVersion(version, new SemanticVersionSource("tag", version, commit, VersionField.None));

        var child = parent.CreateChildContext();

        child.ShouldNotBeSameAs(parent);
        child.TargetLabel.ShouldBe("beta");
        child.BranchPointResolver.ShouldBeSameAs(parent.BranchPointResolver);
        child.IncludeSharedHistory.ShouldBeTrue();
        child.CurrentCommit.ShouldBeSameAs(commit);
        child.MainlineVersions.ShouldBeSameAs(parent.MainlineVersions);
        child.MainlineVersions[iteration].ShouldBeSameAs(cached);
        child.Increment.ShouldBe(VersionField.None);
        child.CommitIncrement.ShouldBe(VersionField.None);
        child.SemanticVersion.ShouldBeNull();
        child.SemVerSource.ShouldBeNull();
        child.BaseVersionSource.ShouldBeNull();
        child.Predecessor.ShouldBeNull();
        child.HasSuccessor.ShouldBeNull();
        child.Label.ShouldBeNull();
        child.ForceIncrement.ShouldBeFalse();
        child.SuppressBranchIncrement.ShouldBeFalse();
        child.AlternativeSemanticVersions.ShouldBeEmpty();
        parent.Increment.ShouldBe(VersionField.Major);
        parent.AlternativeSemanticVersions.ShouldBe([version]);
    }

    [Test]
    public void SelectionStopsAtTheEligibleReleaseInChronologicalOrder()
    {
        var branch = Branch(true);
        var iteration = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var tip = iteration.CreateCommit(Commit(), iteration.BranchName, branch);
        var release = iteration.CreateCommit(Commit(), iteration.BranchName, branch);
        release.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var initial = iteration.CreateCommit(Commit(), iteration.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        tip.Value.Parents.Returns([release.Value]);
        release.Value.Parents.Returns([initial.Value]);
        var recorded = iteration.Commits.ToArray();

        var selected = History(tip.Value).Select(iteration, "", false);

        selected.Commits.ShouldBe([release, tip]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        iteration.Commits.ShouldBe(recorded);
    }

    [Test]
    public void SharedAncestryUsesExistingNodesOnlyForIndependentCalculation()
    {
        var branch = Branch(true);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(Commit(), root.BranchName, branch);
        merge.AddSemanticVersions(new SemanticVersion(3, 0, 0));
        var shared = root.CreateCommit(Commit(), root.BranchName, branch);
        var initial = root.CreateCommit(Commit(), root.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        shared.Value.Parents.Returns([initial.Value]);
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch,
            root, shared.Value, merge.Value);
        var history = History(merge.Value);

        history.Select(source, "", false).Commits.ShouldBeEmpty();
        var independent = history.Select(source, "", true);

        independent.Commits.ShouldBe([initial, shared]);
        independent.Commits[1].ShouldBeSameAs(shared);
        independent.HasBoundary.ShouldBeTrue();
        source.Commits.ShouldBeEmpty();
        root.Commits.ShouldBe([initial, shared, merge]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SelectionPreservesOwnedConfigurationTransitionsAndTheRequestedTip(bool mainline)
    {
        var branch = Branch(mainline);
        var newestValue = Commit();
        var oldestValue = Commit();
        newestValue.Parents.Returns([oldestValue]);
        var iteration = new MainlineIteration("source", new("refs/heads/support/1.x"), branch,
            null, null, null);
        var newest = iteration.CreateCommit(newestValue, iteration.BranchName, branch);
        var transition = iteration.CreateCommit(null, new("refs/heads/main"), branch);
        var oldest = iteration.CreateCommit(oldestValue, iteration.BranchName, branch);
        var history = History(newestValue);

        history.Select(iteration, "", false).Commits.ShouldBe([oldest, transition, newest]);
        history.Select(iteration, "", false, tip: oldest).Commits.ShouldBe([oldest]);
        iteration.Commits.ShouldBe([oldest, transition, newest]);
    }

    [Test]
    public void MissingBranchPointIsReportedWithoutRecordingIt()
    {
        var branch = Branch(true);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(Commit(), root.BranchName, branch);
        var missing = Commit();
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch,
            root, missing, merge.Value);

        var selected = History(merge.Value).Select(source, "", true);

        selected.MissingCommit.ShouldBeSameAs(missing);
        selected.HasBoundary.ShouldBeFalse();
        selected.Commits.ShouldBeEmpty();
        source.Commits.ShouldBeEmpty();
        root.Commits.ShouldBe([merge]);
        root.FindCommit(missing).ShouldBeNull();
    }

    [Test]
    public void ReleasedSourceDoesNotRequireUnrecordedAncestry()
    {
        var missing = Commit();
        var branch = Branch(true);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(Commit(), root.BranchName, branch);
        var source = new MainlineIteration("source", new("refs/heads/support/1.x"), branch, root, missing, merge.Value);
        var release = source.CreateCommit(Commit(), source.BranchName, branch);
        release.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        merge.AddChildIteration(source);

        var selected = History(merge.Value).Select(source, "", true);

        selected.Commits.ShouldBe([release]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
        root.FindCommit(missing).ShouldBeNull();
        source.Commits.ShouldBe([release]);
    }

    [TestCase(false, TestName = "FeatureReleaseDelimitsReceiverHistory")]
    [TestCase(true, TestName = "MainlineReleaseDoesNotDelimitReceiverHistory")]
    public void OnlyFeatureReleaseBoundariesPropagateToTheReceiver(bool mainlineSource)
    {
        var branch = Branch(true);
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var merge = root.CreateCommit(Commit(), root.BranchName, branch);
        var initial = root.CreateCommit(Commit(), root.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var childBranch = Branch(mainlineSource);
        var child = new MainlineIteration("child", new("refs/heads/source"), childBranch, root, initial.Value, merge.Value);
        var childTip = child.CreateCommit(Commit(initial.Value), child.BranchName, childBranch);
        childTip.AddSemanticVersions(new SemanticVersion(2, 0, 0));
        merge.Value.Parents.Returns([initial.Value, childTip.Value]);
        merge.AddChildIteration(child);

        var selected = History(merge.Value).Select(root, "", false);

        selected.Commits.ShouldBe(mainlineSource ? [initial, merge] : [merge]);
        selected.HasBoundary.ShouldBeTrue();
        selected.MissingCommit.ShouldBeNull();
    }

    private static MainlineHistory History(ICommit currentCommit)
        => new(GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(), currentCommit);

    private static BranchConfiguration Branch(bool mainline)
        => new() { Increment = IncrementStrategy.Patch, IsMainBranch = mainline, Label = "" };

    private static ICommit Commit(ICommit? parent = null)
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        ICommit[] parents = parent is null ? [] : [parent];
        commit.Parents.Returns(parents);
        return commit;
    }
}
