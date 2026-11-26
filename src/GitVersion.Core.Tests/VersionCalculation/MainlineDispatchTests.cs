using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;
using GitVersion.VersionCalculation.Mainline.Trunk;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineDispatchTests
{
    [TestCase("ordinary", nameof(CommitOnTrunk))]
    [TestCase("feature-merge", nameof(LastMergeCommitOnTrunk))]
    [TestCase("mainline-merge", nameof(MergedMainlineIncrementer))]
    [TestCase("stable-tag", nameof(LastCommitOnTrunkWithStableTag))]
    [TestCase("prerelease-tag", nameof(LastCommitOnTrunkWithPreReleaseTag))]
    public void PrimaryDispatchSelectsTheCommitScenario(string scenario, string expected)
    {
        var fixture = Fixture(attachChild: scenario != "ordinary", sourceMainline: scenario != "feature-merge");
        fixture.Context.SemanticVersion = scenario switch
        {
            "stable-tag" => SemanticVersion.Parse("4.0.0", null),
            "prerelease-tag" => SemanticVersion.Parse("4.0.0-beta.5", null),
            _ => null
        };
        new EnrichMergeState().Enrich(fixture.Root, fixture.Merge, fixture.Context);
        var (calculator, _, history, sourceVersions, _) = fixture.Calculation;
        var environment = history.Environment;
        IIncrementer[] components =
        [
            new CommitOnTrunk(history, environment), new MergedMainlineIncrementer(history, environment, sourceVersions),
            new MergeCommitOnTrunk(history, calculator), new LastMergeCommitOnTrunk(history, calculator),
            new CommitOnTrunkWithStableTag(environment), new LastCommitOnTrunkWithStableTag(environment),
            new CommitOnTrunkWithPreReleaseTag(), new LastCommitOnTrunkWithPreReleaseTag(environment)
        ];

        var matches = components
            .Where(c => c.MatchPrecondition(fixture.Root, fixture.Merge, fixture.Context)).ToArray();

        matches.Select(c => c.GetType().Name).ShouldBe([expected]);
        fixture.Context.MainlineVersions.ShouldBeEmpty();
    }

    [TestCase(true, false, "3.0.0-beta.1")]
    [TestCase(false, false, "3.0.0-beta.1")]
    [TestCase(true, true, "2.0.0-beta.1")]
    [TestCase(false, true, "2.0.0-beta.1")]
    public void MainlineMergeProjectsItsPrimaryOperationBeforeTheBranchTransition(
        bool enclosingMainline, bool preventSource, string expected)
    {
        var fixture = Fixture(boundary: true, enclosingMainline: enclosingMainline, preventSource: preventSource);
        var context = fixture.Context;
        context.Increments.Add(new BaseVersionOperand("receiver", new SemanticVersion(1, 0, 0), fixture.Initial.Value));
        context.Increments.Add(new BaseVersionOperator
        {
            Increment = VersionField.Patch,
            BaseVersionSource = fixture.Initial.Value
        });
        var (_, _, history, sourceVersions, _) = fixture.Calculation;
        var environment = history.Environment;
        using var primary = new MergedMainlineIncrementer(history, environment, sourceVersions)
            .GetIncrements(fixture.Root, fixture.Merge, context).GetEnumerator();

        primary.MoveNext().ShouldBeTrue();
        context.BaseVersionSource.ShouldBeSameAs(fixture.Initial.Value);
        context.Increments.Add(primary.Current);
        context.Increments.Version.ShouldNotBeNull().GetIncrementedVersion().ToString()
            .ShouldBe(preventSource ? "1.0.2-1" : "2.0.0-1");
        primary.MoveNext().ShouldBeFalse();
        context.BaseVersionSource.ShouldBeSameAs(fixture.Merge.Value);
        IIncrementer[] transitions =
        [
            new CommitOnTrunkBranchedToTrunk(environment), new MergedMainlineBranchedToTrunk(environment, sourceVersions),
            new CommitOnTrunkBranchedToNonTrunk(environment), new MergedMainlineBranchedToNonTrunk(environment, sourceVersions)
        ];
        var transition = transitions.Single(c => c.MatchPrecondition(fixture.Root, fixture.Merge, context));
        transition.GetType().Name.ShouldBe(enclosingMainline
            ? nameof(MergedMainlineBranchedToTrunk) : nameof(MergedMainlineBranchedToNonTrunk));

        var items = transition.GetIncrements(fixture.Root, fixture.Merge, context).ToArray();
        items.Length.ShouldBe(1);
        context.Increments.Add(items[0]);

        var result = context.Increments.Version.ShouldNotBeNull();
        result.GetIncrementedVersion().ToString().ShouldBe(expected);
        result.Operator.ShouldNotBeNull().Increment.ShouldBe(VersionField.Major);
        result.Operator.ForceIncrement.ShouldBeTrue();
        result.Operator.Label.ShouldBe("beta");
        result.Source.ShouldBe(enclosingMainline
            ? nameof(CommitOnTrunkBranchedToTrunk) : nameof(CommitOnTrunkBranchedToNonTrunk));
        result.BaseVersionSource.ShouldBeSameAs(fixture.Merge.Value);
        result.GetSemVerSource().Commit.ShouldBeSameAs(preventSource ? fixture.Initial.Value : fixture.SourceValue);
        result.GetSemVerSource().Increment.ShouldBe(VersionField.Major);
        context.Increments.ReleaseBaseline.ShouldNotBeNull().ToString().ShouldBe("1.0.0");
        context.Label.ShouldBe("beta");
        context.Increment.ShouldBe(VersionField.Major);
        context.MainlineVersions.Count.ShouldBe(preventSource ? 0 : 1);
    }

    [TestCase(true, false, "3.0.0-beta.1")]
    [TestCase(false, false, "3.0.0-beta.1")]
    [TestCase(true, true, "2.0.0-beta.1")]
    [TestCase(false, true, "2.0.0-beta.1")]
    public void RegisteredComponentsPreserveTheMergeAndEnclosingBranch(
        bool enclosingMainline, bool preventSource, string expected)
    {
        var fixture = Fixture(boundary: true, enclosingMainline: enclosingMainline, preventSource: preventSource);

        var result = fixture.Calculation.Calculator.DetermineBaseVersion(fixture.Root, null);

        result.GetIncrementedVersion().ToString().ShouldBe(expected);
        result.BaseVersionSource.ShouldBeSameAs(fixture.Merge.Value);
        result.GetSemVerSource().Commit.ShouldBeSameAs(preventSource ? fixture.Initial.Value : fixture.SourceValue);
        result.GetSemVerSource().Increment.ShouldBe(VersionField.Major);
    }

    [TestCase(true, "none")]
    [TestCase(false, "none")]
    [TestCase(true, "4.0.0")]
    [TestCase(false, "4.0.0")]
    [TestCase(true, "4.0.0-beta.5")]
    [TestCase(false, "4.0.0-beta.5")]
    public void OrdinaryAndTaggedBoundariesKeepTheirOriginalComponents(bool enclosingMainline, string tag)
    {
        var fixture = Fixture(boundary: true, enclosingMainline: enclosingMainline, attachChild: tag != "none");
        fixture.Context.SemanticVersion = tag == "none" ? null : SemanticVersion.Parse(tag, null);
        new EnrichMergeState().Enrich(fixture.Root, fixture.Merge, fixture.Context);
        var (_, _, history, sourceVersions, _) = fixture.Calculation;
        var environment = history.Environment;
        IIncrementer[] transitions =
        [
            new CommitOnTrunkBranchedToTrunk(environment), new MergedMainlineBranchedToTrunk(environment, sourceVersions),
            new CommitOnTrunkBranchedToNonTrunk(environment), new MergedMainlineBranchedToNonTrunk(environment, sourceVersions)
        ];

        var transition = transitions.Single(c => c.MatchPrecondition(fixture.Root, fixture.Merge, fixture.Context));
        var items = transition.GetIncrements(fixture.Root, fixture.Merge, fixture.Context).ToArray();

        transition.GetType().Name.ShouldBe(enclosingMainline
            ? nameof(CommitOnTrunkBranchedToTrunk) : nameof(CommitOnTrunkBranchedToNonTrunk));
        items.Length.ShouldBe(1);
        var operation = items[0].ShouldBeOfType<BaseVersionOperator>();
        operation.Increment.ShouldBe(VersionField.Major);
        operation.Label.ShouldBe("beta");
        operation.ForceIncrement.ShouldBeTrue();
        operation.BaseVersionSource.ShouldBeSameAs(fixture.Merge.Value);
        fixture.Context.MainlineVersions.ShouldBeEmpty();
    }

    [TestCase(true, "not-last")]
    [TestCase(false, "not-last")]
    [TestCase(true, "same-branch")]
    [TestCase(false, "same-branch")]
    [TestCase(true, "empty-iteration")]
    [TestCase(false, "empty-iteration")]
    public void MainlineTransitionsRequireTheOriginalBranchBoundary(bool enclosingMainline, string reason)
    {
        var fixture = Fixture(boundary: reason != "same-branch", enclosingMainline: enclosingMainline);
        fixture.Context.HasSuccessor = reason == "not-last";
        var iteration = reason == "empty-iteration"
            ? new MainlineIteration("empty", fixture.Root.BranchName, fixture.Root.Configuration, null, null, null)
            : fixture.Root;

        var (_, _, history, sourceVersions, _) = fixture.Calculation;
        var environment = history.Environment;
        new MergedMainlineBranchedToTrunk(environment, sourceVersions)
            .MatchPrecondition(iteration, fixture.Merge, fixture.Context).ShouldBeFalse();
        new MergedMainlineBranchedToNonTrunk(environment, sourceVersions)
            .MatchPrecondition(iteration, fixture.Merge, fixture.Context).ShouldBeFalse();
        fixture.Context.MainlineVersions.ShouldBeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptySourceHistoryStillSelectsTheMainlineMerge(bool inheritSource)
    {
        var fixture = Fixture(emptySource: true, inheritSource: inheritSource);
        fixture.Context.Increments.Add(
            new BaseVersionOperand("receiver", new SemanticVersion(1, 0, 0), fixture.Initial.Value));
        fixture.Context.Increments.Add(new BaseVersionOperator { Increment = VersionField.Patch });
        var (_, _, history, sourceVersions, _) = fixture.Calculation;
        var environment = history.Environment;
        var incrementer = new MergedMainlineIncrementer(history, environment, sourceVersions);

        incrementer.MatchPrecondition(fixture.Root, fixture.Merge, fixture.Context).ShouldBeTrue();
        new CommitOnTrunk(history, environment).MatchPrecondition(fixture.Root, fixture.Merge, fixture.Context).ShouldBeFalse();
        var items = incrementer.GetIncrements(fixture.Root, fixture.Merge, fixture.Context).ToArray();

        items.Length.ShouldBe(1);
        var result = items[0].ShouldBeOfType<BaseVersion>();
        result.GetIncrementedVersion().ToString().ShouldBe("1.0.2-1");
        result.GetSemVerSource().Commit.ShouldBeSameAs(fixture.Initial.Value);
        fixture.Context.MainlineVersions.Count.ShouldBe(inheritSource ? 0 : 1);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BranchNameFloorWinsOverBothMainlineVersions(bool enclosingMainline)
    {
        var fixture = Fixture(boundary: true, enclosingMainline: enclosingMainline, releaseFloor: true);

        var result = fixture.Calculation.Calculator.DetermineBaseVersion(fixture.Root, null);

        result.GetIncrementedVersion().ToString().ShouldBe("4.0.0-beta.1");
        result.BaseVersionSource.ShouldBeSameAs(fixture.Merge.Value);
        result.GetSemVerSource().Description.ShouldBe("Version in branch name");
        result.GetSemVerSource().Commit.ShouldBeNull();
        result.GetSemVerSource().Increment.ShouldBe(VersionField.None);
    }

    private static DispatchFixture Fixture(bool boundary = false, bool enclosingMainline = true,
        bool preventSource = false, bool attachChild = true, bool sourceMainline = true,
        bool emptySource = false, bool inheritSource = false, bool releaseFloor = false)
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var enclosing = boundary
            ? new BranchConfiguration
            {
                Increment = IncrementStrategy.Major,
                IsMainBranch = enclosingMainline,
                IsReleaseBranch = releaseFloor,
                Label = "beta"
            } : branch;
        var enclosingName = releaseFloor ? "refs/heads/release/4.0.0" : "refs/heads/enclosing";
        var name = boundary ? enclosingName : "refs/heads/main";
        var root = new MainlineIteration("root", new(name), enclosing, null, null, null);
        var initialValue = Commit();
        var sourceValue = emptySource ? initialValue : Commit();
        var receiverValue = emptySource ? Commit() : initialValue;
        if (emptySource)
        {
            receiverValue.Parents.Returns([initialValue]);
        }
        else
        {
            sourceValue.Parents.Returns([initialValue]);
        }
        var mergeValue = Commit();
        mergeValue.Parents.Returns([receiverValue, sourceValue]);
        var merge = root.CreateCommit(mergeValue, new("refs/heads/main"), branch);
        if (emptySource)
        {
            root.CreateCommit(receiverValue, merge.BranchName, branch);
        }
        var initial = root.CreateCommit(initialValue, merge.BranchName, branch);
        initial.AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var sourceBranch = new BranchConfiguration
        {
            Increment = inheritSource ? IncrementStrategy.Inherit : IncrementStrategy.Patch,
            IsMainBranch = sourceMainline,
            Label = "",
            PreventIncrement = new() { WhenBranchMerged = preventSource, WhenCurrentCommitTagged = true }
        };
        var source = new MainlineIteration("source", new("refs/heads/support/source"), sourceBranch,
            root, initialValue, mergeValue);
        if (!emptySource)
        {
            source.CreateCommit(sourceValue, source.BranchName, sourceBranch)
                .AddSemanticVersions(new SemanticVersion(2, 0, 0));
        }
        if (attachChild)
        {
            merge.AddChildIteration(source);
        }
        var calculation = MainlineTestCalculation.Create(root, GitFlowConfigurationBuilder.New.Build(),
            new TestEnvironment(), mergeValue);
        var context = calculation.Context;
        context.Predecessor = initial;
        context.HasSuccessor = false;
        context.BaseVersionSource = initialValue;
        context.Increment = VersionField.Patch;
        new EnrichMergeState().Enrich(root, merge, context);
        return new(root, merge, initial, calculation, sourceValue);
    }

    private static ICommit Commit()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        return commit;
    }

    private sealed record DispatchFixture(MainlineIteration Root, MainlineCommit Merge, MainlineCommit Initial,
        MainlineTestCalculation.Components Calculation, ICommit SourceValue)
    {
        public MainlineContext Context => Calculation.Context;
    }
}
