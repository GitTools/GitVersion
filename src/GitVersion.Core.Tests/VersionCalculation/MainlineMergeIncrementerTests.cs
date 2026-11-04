using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineMergeIncrementerTests
{
    [TestCase("1.0.0", "2.0.0", null, "2.0.0-1", "source")]
    [TestCase("3.0.0", "2.0.0", null, "3.0.0-1", "receiver")]
    [TestCase("2.0.0", "2.0.0", null, "2.0.0-1", "receiver")]
    [TestCase("1.0.0", "2.0.0", "3.0.0", "3.0.0-1", "existing")]
    [TestCase("1.0.0", "2.0.0", "2.0.0", "2.0.0-1", "existing")]
    [TestCase("1.0.0", "2.0.0", "1.5.0", "2.0.0-1", "source")]
    public void ResolvedMergePreservesVersionWinnerAndReceiverCountingSource(
        string receiverText, string sourceText, string? alternativeText, string expected, string winner)
    {
        var receiverCommit = Commit();
        var sourceCommit = Commit();
        var alternativeCommit = Commit();
        var receiver = new BaseVersion("receiver", SemanticVersion.Parse(receiverText, null), receiverCommit);
        var source = new BaseVersion("source", SemanticVersion.Parse(sourceText, null), sourceCommit);
        var alternative = alternativeText is null ? null : SemanticVersion.Parse(alternativeText, null);
        var operation = new BaseVersionOperator
        {
            Source = "merge",
            BaseVersionSource = receiverCommit,
            AlternativeSemanticVersion = alternative,
            AlternativeSemVerSource = alternative is null ? null
                : new SemanticVersionSource("existing", alternative, alternativeCommit, VersionField.None)
        };

        var result = MergedMainlineIncrementer.Resolve(receiver, operation, source, receiver.SemanticVersion);

        result.GetIncrementedVersion().ToString().ShouldBe(expected);
        result.BaseVersionSource.ShouldBeSameAs(receiverCommit);
        var origin = result.GetSemVerSource();
        origin.Commit.ShouldBeSameAs(winner switch
        {
            "source" => sourceCommit,
            "existing" => alternativeCommit,
            _ => receiverCommit
        });
        origin.Increment.ShouldBe(VersionField.None);
        if (winner == "existing")
        {
            var resultOperator = result.Operator.ShouldNotBeNull();
            resultOperator.ShouldBeSameAs(operation);
            resultOperator.AlternativeSemVerSource.ShouldBeSameAs(operation.AlternativeSemVerSource);
        }
    }

    [TestCase("2.0.0-5", "2.0.0-5", "", "2.0.0-6")]
    [TestCase("2.0.0-5", null, "", "2.0.0-1")]
    [TestCase("3.0.0-5", "2.0.0-5", "", "3.0.0-1")]
    [TestCase("2.0.0-alpha.5", "2.0.0-alpha.5", "alpha", "2.0.0-alpha.6")]
    public void ReleasedPrereleaseContinuesWhileGeneratedUnnamedSuffixRestarts(
        string receiverText, string? baselineText, string label, string expected)
    {
        var receiver = new BaseVersion("receiver", SemanticVersion.Parse(receiverText, null), Commit());
        var source = new BaseVersion("source", new SemanticVersion(1, 0, 0), Commit());
        var operation = new BaseVersionOperator { ForceIncrement = true, Label = label };

        var result = MergedMainlineIncrementer.Resolve(receiver, operation, source,
            baselineText is null ? null : SemanticVersion.Parse(baselineText, null));

        result.GetIncrementedVersion().ToString().ShouldBe(expected);
        result.GetSemVerSource().Commit.ShouldBeSameAs(receiver.BaseVersionSource);
    }

    [Test]
    public void MergeMaterializesTheReceiversPendingIncrementBeforeItsNextOperator()
    {
        var commit = Commit();
        var state = new MainlineIncrementState();
        state.Add(new BaseVersionOperand("receiver", new SemanticVersion(1, 0, 0), commit));
        state.Add(new BaseVersionOperator { Increment = VersionField.Patch, BaseVersionSource = commit });
        var operation = new BaseVersionOperator { Increment = VersionField.Patch, BaseVersionSource = commit };

        var result = MergedMainlineIncrementer.Resolve(state.Version.ShouldNotBeNull(), operation,
            new BaseVersion("lower", new SemanticVersion(0, 1, 0)), state.ReleaseBaseline);

        result.SemanticVersion.ToString().ShouldBe("1.0.1");
        result.GetIncrementedVersion().ToString().ShouldBe("1.0.2-1");
        result.GetSemVerSource().Commit.ShouldBeSameAs(commit);
        result.GetSemVerSource().Increment.ShouldBe(VersionField.Patch);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DecoratorDelegatesMatchingToItsWrappedIncrementer(bool matches)
    {
        var fixture = Fixture();
        var inner = new StubIncrementer(matches);
        var decorator = new MergedMainlineIncrementer(inner);

        decorator.MatchPrecondition(fixture.Root, fixture.Merge, fixture.Context).ShouldBe(matches);

        inner.MatchCalls.ShouldBe(1);
        inner.LastIteration.ShouldBeSameAs(fixture.Root);
        inner.LastCommit.ShouldBeSameAs(fixture.Merge);
        inner.LastContext.ShouldBeSameAs(fixture.Context);
        inner.IncrementCalls.ShouldBe(0);
    }

    [TestCase("not-mainline")]
    [TestCase("tagged")]
    [TestCase("prevent-source")]
    [TestCase("no-child")]
    [TestCase("operand")]
    public void InapplicableIncrementsPassThroughWithoutCalculatingASource(string reason)
    {
        var fixture = Fixture(preventSource: reason == "prevent-source", attachChild: reason != "no-child");
        fixture.Context.IsMainlineMerge = reason != "not-mainline";
        if (reason == "tagged")
        {
            fixture.Context.SemanticVersion = new SemanticVersion(9, 0, 0);
        }
        IBaseVersionIncrement item = reason == "operand"
            ? new BaseVersionOperand("tag", new SemanticVersion(1, 0, 0))
            : new BaseVersionOperator { Increment = VersionField.Patch };
        var inner = new StubIncrementer(true, item);

        var result = new MergedMainlineIncrementer(inner)
            .GetIncrements(fixture.Root, fixture.Merge, fixture.Context).ToArray();

        result.Length.ShouldBe(1);
        result[0].ShouldBeSameAs(item);
        inner.IncrementCalls.ShouldBe(1);
        inner.LastIteration.ShouldBeSameAs(fixture.Root);
        inner.LastCommit.ShouldBeSameAs(fixture.Merge);
        inner.LastContext.ShouldBeSameAs(fixture.Context);
        fixture.Context.MainlineVersions.ShouldBeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SourceCacheUsesOnlyExplicitIterationConfiguration(bool inherit)
    {
        var fixture = Fixture(inherit: inherit);
        var parentVersion = new BaseVersion("parent", new SemanticVersion(9, 0, 0));
        fixture.Context.Increments.Add(parentVersion);

        var first = MergedMainlineIncrementer.GetSourceVersion(fixture.Source, fixture.Context);
        var second = MergedMainlineIncrementer.GetSourceVersion(fixture.Source, fixture.Context);

        first.SemanticVersion.ToString().ShouldBe("2.0.0");
        second.GetIncrementedVersion().ToString().ShouldBe("2.0.0-1");
        first.GetSemVerSource().Commit.ShouldBeSameAs(fixture.SourceValue);
        fixture.Context.Increments.Version.ShouldBeSameAs(parentVersion);
        if (inherit)
        {
            second.ShouldNotBeSameAs(first);
            fixture.Context.MainlineVersions.ShouldBeEmpty();
        }
        else
        {
            second.ShouldBeSameAs(first);
            fixture.Context.MainlineVersions.Count.ShouldBe(1);
        }
    }

    [TestCase(true, VersionField.None, "2.0.0-1")]
    [TestCase(false, VersionField.Patch, "2.0.1-1")]
    public void ActualReleasedSourceTipHonorsTaggedIncrementPrevention(
        bool preventTaggedIncrement, VersionField increment, string expected)
    {
        var fixture = Fixture(preventTaggedIncrement: preventTaggedIncrement);

        var result = MergedMainlineIncrementer.GetSourceVersion(fixture.Source, fixture.Context);

        result.GetIncrementedVersion().ToString().ShouldBe(expected);
        result.Operator.ShouldNotBeNull().Increment.ShouldBe(increment);
        result.Operand.BaseVersionSource.ShouldBeSameAs(fixture.SourceValue);
        result.GetSemVerSource().Commit.ShouldBeSameAs(fixture.SourceValue);
        result.GetSemVerSource().Increment.ShouldBe(increment);
    }

    [Test]
    public void IndependentSourceUsesItsOwnLabelAndFreshProjection()
    {
        var fixture = Fixture(label: "beta");
        fixture.Context.Increments.Add(new BaseVersion("receiver", new SemanticVersion(9, 0, 0)));

        var result = MergedMainlineIncrementer.GetSourceVersion(fixture.Source, fixture.Context);

        result.SemanticVersion.ToString().ShouldBe("2.0.0-beta.4");
        result.GetIncrementedVersion().ToString().ShouldBe("2.0.0-beta.5");
        result.GetSemVerSource().Commit.ShouldBeSameAs(fixture.SourceValue);
        fixture.Context.TargetLabel.ShouldBe("receiver");
        fixture.Context.Increments.Version.ShouldNotBeNull().SemanticVersion.Major.ShouldBe(9);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SourceLabelUsesTheHistorySelectedParent(bool swapped)
    {
        var fixture = Fixture(label: "ci.{Sha}", swapped: swapped);
        var sourceLabel = "ci." + fixture.SourceValue.Sha;
        var otherTip = fixture.Merge.Value.Parents[swapped ? 1 : 0];
        var otherLabel = "ci." + otherTip.Sha;
        fixture.Source.Commits.Single().AddSemanticVersions(
            SemanticVersion.Parse($"2.0.0-{sourceLabel}.4", null),
            SemanticVersion.Parse($"2.0.0-{otherLabel}.4", null));
        fixture.Context.IsMainlineMerge.ShouldBeTrue();

        var result = MergedMainlineIncrementer.GetSourceVersion(fixture.Source, fixture.Context);

        result.Operand.SemanticVersion.PreReleaseTag.Name.ShouldBe(sourceLabel);
        result.Operand.BaseVersionSource.ShouldBeSameAs(fixture.SourceValue);
        fixture.Context.TargetLabel.ShouldBe("receiver");
    }

    [Test]
    public void MergeEligibilityIsResetForEachCommit()
    {
        var fixture = Fixture();
        var enricher = new EnrichMergeState();
        enricher.Enrich(fixture.Root, fixture.Merge, fixture.Context);
        fixture.Context.IsMainlineMerge.ShouldBeTrue();
        var ordinary = fixture.Root.CreateCommit(Commit(), fixture.Root.BranchName, fixture.Root.Configuration);

        enricher.Enrich(fixture.Root, ordinary, fixture.Context);

        fixture.Context.IsMainlineMerge.ShouldBeFalse();
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    [TestCase(false, true, false)]
    public void UnsupportedMergeValidationRetainsTheExplicitReleaseBoundary(
        bool inherit, bool tagged, bool reject)
    {
        var branch = new BranchConfiguration
        {
            Increment = inherit ? IncrementStrategy.Inherit : IncrementStrategy.Patch
        };
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var value = Commit();
        ICommit[] parents = [Commit(), Commit(), Commit()];
        value.Parents.Returns(parents);
        var commit = root.CreateCommit(value, root.BranchName, branch);
        var context = MainlineTestCalculation.Context(root, GitFlowConfigurationBuilder.New.Build(),
            new TestEnvironment(), value);
        context.SemanticVersion = tagged ? new SemanticVersion(2, 0, 0) : null;
        var enricher = new EnrichMergeState();

        if (reject)
        {
            Should.Throw<NotSupportedException>(() => enricher.Enrich(root, commit, context));
        }
        else
        {
            Should.NotThrow(() => enricher.Enrich(root, commit, context));
        }
    }

    private static MergeFixture Fixture(bool inherit = false, bool preventSource = false, bool attachChild = true,
        bool preventTaggedIncrement = true, string label = "", bool swapped = false)
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var sourceBranch = new BranchConfiguration
        {
            Increment = inherit || swapped ? IncrementStrategy.Inherit : IncrementStrategy.Patch,
            IsMainBranch = swapped ? null : true,
            Label = label,
            PreventIncrement = new()
            {
                WhenBranchMerged = preventSource,
                WhenCurrentCommitTagged = preventTaggedIncrement
            }
        };
        var root = new MainlineIteration("root", new("refs/heads/main"), branch, null, null, null);
        var initialValue = Commit();
        var sourceValue = Commit();
        sourceValue.Parents.Returns([initialValue]);
        var mergeValue = Commit();
        mergeValue.Parents.Returns(swapped ? [sourceValue, initialValue] : [initialValue, sourceValue]);
        var merge = root.CreateCommit(mergeValue, root.BranchName, branch);
        root.CreateCommit(initialValue, root.BranchName, branch).AddSemanticVersions(new SemanticVersion(1, 0, 0));
        var sourceBranchName = swapped ? root.BranchName : new ReferenceName("refs/heads/support/source");
        var source = new MainlineIteration("source", sourceBranchName, sourceBranch,
            root, initialValue, mergeValue);
        source.CreateCommit(sourceValue, source.BranchName, sourceBranch)
            .AddSemanticVersions(SemanticVersion.Parse(label == "" ? "2.0.0" : "2.0.0-beta.4", null));
        if (attachChild)
        {
            merge.AddChildIteration(source);
        }
        var context = MainlineTestCalculation.Context(root, GitFlowConfigurationBuilder.New.Build(),
            new TestEnvironment(), mergeValue) with
        { TargetLabel = "receiver" };
        new EnrichMergeState().Enrich(root, merge, context);
        return new(root, merge, source, context, sourceValue);
    }

    private sealed record MergeFixture(MainlineIteration Root, MainlineCommit Merge, MainlineIteration Source,
        MainlineContext Context, ICommit SourceValue);

    private sealed class StubIncrementer(bool matches, params IBaseVersionIncrement[] increments) : IIncrementer
    {
        internal int MatchCalls { get; private set; }
        internal int IncrementCalls { get; private set; }
        internal MainlineIteration? LastIteration { get; private set; }
        internal MainlineCommit? LastCommit { get; private set; }
        internal MainlineContext? LastContext { get; private set; }

        public bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        {
            MatchCalls++;
            LastIteration = iteration;
            LastCommit = commit;
            LastContext = context;
            return matches;
        }

        public IEnumerable<IBaseVersionIncrement> GetIncrements(
            MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        {
            IncrementCalls++;
            LastIteration = iteration;
            LastCommit = commit;
            LastContext = context;
            return increments;
        }
    }

    private static ICommit Commit()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        return commit;
    }
}
