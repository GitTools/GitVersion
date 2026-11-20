using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineConfigurationResolverTests
{
    [Test]
    public void LocalIterationConfigurationTakesPriorityOverItsBranchPoint()
    {
        var receiverBranch = Branch(IncrementStrategy.Patch, "stable");
        var root = Iteration("root", receiverBranch);
        var merge = root.CreateCommit(Commit(), root.BranchName, receiverBranch);
        var branchPoint = root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Major, "alpha"));
        var inherited = new BranchConfiguration { Increment = IncrementStrategy.Inherit };
        var child = Iteration("child", inherited, root, branchPoint.Value, merge.Value);
        var local = child.CreateCommit(Commit(), child.BranchName, Branch(IncrementStrategy.Minor, "beta"));
        merge.AddChildIteration(child);
        var resolver = Resolver(root);

        var effective = resolver.GetEffectiveConfiguration(child);

        effective.Increment.ShouldBe(IncrementStrategy.Minor);
        effective.Label.ShouldBe("beta");
        resolver.GetEffectiveConfiguration(local).Increment.ToVersionField().ShouldBe(VersionField.Minor);
        child.Configuration.Increment.ShouldBe(IncrementStrategy.Inherit);
        child.Commits.ShouldBe([local]);
        branchPoint.Iteration.ShouldBeSameAs(root);
    }

    [Test]
    public void CommitInheritanceStopsAtTheFirstExplicitPredecessor()
    {
        var inherited = new BranchConfiguration { Increment = IncrementStrategy.Inherit };
        var root = Iteration("root", inherited);
        var tip = root.CreateCommit(Commit(), root.BranchName, inherited);
        var transition = root.CreateCommit(null, root.BranchName, Branch(IncrementStrategy.Minor, "beta"));
        var initial = root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Major, "alpha"));
        var resolver = Resolver(root);

        var effective = resolver.GetEffectiveConfiguration(tip);

        effective.Increment.ShouldBe(IncrementStrategy.Minor);
        effective.Label.ShouldBe("beta");
        resolver.GetEffectiveConfiguration(tip).Increment.ToVersionField().ShouldBe(VersionField.Minor);
        resolver.GetEffectiveConfiguration(tip).ShouldBeSameAs(effective);
        tip.Configuration.ShouldBeSameAs(inherited);
        tip.Configuration.Increment.ShouldBe(IncrementStrategy.Inherit);
        tip.Predecessor.ShouldBeSameAs(transition);
        transition.Predecessor.ShouldBeSameAs(initial);
    }

    [Test]
    public void CommitInheritanceUsesTheReceivingMergeRatherThanTheBranchPoint()
    {
        var receiverBranch = Branch(IncrementStrategy.Patch, "stable");
        var root = Iteration("root", receiverBranch);
        var merge = root.CreateCommit(Commit(), root.BranchName, receiverBranch);
        var branchPoint = root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Major, "alpha"));
        var inherited = new BranchConfiguration { Increment = IncrementStrategy.Inherit };
        var child = Iteration("child", inherited, root, branchPoint.Value, merge.Value);
        var source = child.CreateCommit(Commit(), child.BranchName, inherited);
        merge.AddChildIteration(child);
        var resolver = Resolver(root);

        var effective = resolver.GetEffectiveConfiguration(source);

        effective.Increment.ShouldBe(IncrementStrategy.Patch);
        effective.Label.ShouldBe("stable");
        effective.IsMainBranch.ShouldBeTrue();
        resolver.GetEffectiveConfiguration(child).Increment.ShouldBe(IncrementStrategy.Patch);
        resolver.GetEffectiveConfiguration(branchPoint).Increment.ShouldBe(IncrementStrategy.Major);
        source.Iteration.ShouldBeSameAs(child);
        source.Predecessor.ShouldBeNull();
    }

    [Test]
    public void ConfigurationCachesAreIsolatedBetweenCalculations()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var root = Iteration("root", branch);
        var commit = root.CreateCommit(Commit(), root.BranchName, branch);
        var branchPoints = new MainlineCommitResolver(root);
        var first = new MainlineConfigurationResolver(GitFlowConfigurationBuilder.New.WithLabel("alpha").Build(), branchPoints);
        var second = new MainlineConfigurationResolver(GitFlowConfigurationBuilder.New.WithLabel("beta").Build(), branchPoints);

        var firstCommit = first.GetEffectiveConfiguration(commit);
        var firstIteration = first.GetEffectiveConfiguration(root);
        var secondCommit = second.GetEffectiveConfiguration(commit);
        var secondIteration = second.GetEffectiveConfiguration(root);

        firstCommit.Label.ShouldBe("alpha");
        firstIteration.Label.ShouldBe("alpha");
        secondCommit.Label.ShouldBe("beta");
        secondIteration.Label.ShouldBe("beta");
        first.GetEffectiveConfiguration(commit).ShouldBeSameAs(firstCommit);
        first.GetEffectiveConfiguration(root).ShouldBeSameAs(firstIteration);
        second.GetEffectiveConfiguration(commit).ShouldBeSameAs(secondCommit);
        second.GetEffectiveConfiguration(root).ShouldBeSameAs(secondIteration);
        secondCommit.ShouldNotBeSameAs(firstCommit);
        secondIteration.ShouldNotBeSameAs(firstIteration);
        commit.Configuration.ShouldBeSameAs(branch);
        branch.Label.ShouldBeNull();
    }

    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void MergeClassificationRequiresBothEffectiveMainlineBranches(bool receiverMainline, bool sourceMainline, bool expected)
    {
        var receiverBranch = Branch(IncrementStrategy.Patch, "", receiverMainline);
        var root = Iteration("root", receiverBranch);
        var merge = root.CreateCommit(Commit(), root.BranchName, receiverBranch);
        var sourceBranch = Branch(IncrementStrategy.Patch, "", sourceMainline);
        var child = Iteration("child", sourceBranch, root, Commit(), merge.Value);
        merge.AddChildIteration(child);

        var context = MainlineTestCalculation.Context(root, GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(), merge.Value);
        new ConfigurationPreEnricher().Enrich(root, merge, context);
        new EnrichMergeState().Enrich(root, merge, context);
        context.IsMainlineMerge.ShouldBe(expected);
    }

    [Test]
    public void EmptyChildIsClassifiedUsingInheritedConfiguration()
    {
        var branch = Branch(IncrementStrategy.Minor, "beta");
        var root = Iteration("root", branch);
        var merge = root.CreateCommit(Commit(), root.BranchName, branch);
        var branchPoint = root.CreateCommit(Commit(), root.BranchName, branch);
        var child = Iteration("child", new BranchConfiguration { Increment = IncrementStrategy.Inherit }, root, branchPoint.Value, merge.Value);
        merge.AddChildIteration(child);
        var resolver = Resolver(root);

        merge.HasChildIteration.ShouldBeFalse();
        child.Configuration.IsMainBranch.ShouldBeNull();
        resolver.GetEffectiveConfiguration(child).IsMainBranch.ShouldBeTrue();
        var context = MainlineTestCalculation.Context(root, GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(), merge.Value);
        new ConfigurationPreEnricher().Enrich(root, merge, context);
        new EnrichMergeState().Enrich(root, merge, context);
        context.IsMainlineMerge.ShouldBeTrue();
        child.Commits.ShouldBeEmpty();
    }

    [Test]
    public void RootConfigurationAndMergeWithoutAChildNeedNoBranchPoint()
    {
        var branch = Branch(IncrementStrategy.Major, "alpha");
        var root = Iteration("root", branch);
        var commit = root.CreateCommit(Commit(), root.BranchName, branch);
        var resolver = Resolver(root);
        resolver.GetEffectiveConfiguration(root).Increment.ShouldBe(IncrementStrategy.Major);
        resolver.GetEffectiveConfiguration(root).Label.ShouldBe("alpha");

        var context = MainlineTestCalculation.Context(root, GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(), commit.Value);
        new ConfigurationPreEnricher().Enrich(root, commit, context);
        new EnrichMergeState().Enrich(root, commit, context);
        context.IsMainlineMerge.ShouldBeFalse();
        resolver.GetEffectiveConfiguration(commit).Increment.ToVersionField().ShouldBe(VersionField.Major);
    }

    [Test]
    public void ChildContextsShareConfigurationResolutionButKeepCalculationStateIndependent()
    {
        var branch = Branch(IncrementStrategy.Minor, "beta");
        var root = Iteration("root", branch);
        var commit = root.CreateCommit(Commit(), root.BranchName, branch);
        var configuration = GitFlowConfigurationBuilder.New.Build();
        var context = MainlineTestCalculation.Context(root, configuration, new TestEnvironment(), commit.Value) with
        {
            TargetLabel = "target",
            IncludeSharedHistory = true,
            Label = "calculated",
            Increment = VersionField.Major,
            CommitIncrement = VersionField.Minor,
            SemanticVersion = new SemanticVersion(3, 0, 0),
            SemVerSource = new SemanticVersionSource("parent", new SemanticVersion(3, 0, 0), commit.Value, VersionField.Major),
            BaseVersionSource = commit.Value,
            HasSuccessor = true
        };
        context.AddAlternativeSemanticVersion(new SemanticVersion(3, 1, 0), context.SemVerSource);
        var resolver = context.ConfigurationResolver;
        var effective = context.GetEffectiveConfiguration(commit);

        var child = context.CreateChildContext();

        child.ConfigurationResolver.ShouldBeSameAs(resolver);
        child.CommitResolver.ShouldBeSameAs(context.CommitResolver);
        child.GetEffectiveConfiguration(commit).ShouldBeSameAs(effective);
        child.MainlineVersions.ShouldBeSameAs(context.MainlineVersions);
        child.TargetLabel.ShouldBe("target");
        child.IncludeSharedHistory.ShouldBeTrue();
        child.Label.ShouldBeNull();
        child.SemanticVersion.ShouldBeNull();
        child.SemVerSource.ShouldBeNull();
        child.BaseVersionSource.ShouldBeNull();
        child.HasSuccessor.ShouldBeNull();
        child.Increment.ShouldBe(VersionField.None);
        child.CommitIncrement.ShouldBe(VersionField.None);
        child.AlternativeSemanticVersions.ShouldBeEmpty();
        child.AlternativeSemanticVersions.Add(new SemanticVersion(4, 0, 0));
        context.AlternativeSemanticVersions.ShouldBe([new SemanticVersion(3, 1, 0)]);
        context.Label.ShouldBe("calculated");
        context.Increment.ShouldBe(VersionField.Major);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CommitsWithUnchangedSettingsReuseEffectiveConfiguration(bool inherit)
    {
        var branch = new CountingBranchConfiguration
        {
            Increment = inherit ? IncrementStrategy.Inherit : IncrementStrategy.Minor,
            IsMainBranch = true
        };
        var root = Iteration("root", branch);
        for (var index = 0; index < 100; index++)
        {
            root.CreateCommit(Commit(), root.BranchName, branch);
        }
        if (inherit)
        {
            root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Minor, "beta"));
        }
        var resolver = Resolver(root);
        var first = resolver.GetEffectiveConfiguration(root.Commits.Last());

        foreach (var commit in root.Commits.Where(commit => ReferenceEquals(commit.Configuration, branch)))
        {
            resolver.GetEffectiveConfiguration(commit).ShouldBeSameAs(first);
        }
        resolver.GetEffectiveConfiguration(root).ShouldBeSameAs(first);
        first.Increment.ShouldBe(IncrementStrategy.Minor);
        branch.InheritanceCount.ShouldBe(1);
    }

    [Test]
    public void InheritedSettingsKeepNearestOverridesAcrossConfigurationTransitions()
    {
        var inherited = new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration { OfMergedBranch = true }
        };
        var root = Iteration("root", inherited);
        var tip = root.CreateCommit(Commit(), root.BranchName, inherited);
        root.CreateCommit(null, root.BranchName, new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            Label = "nearest",
            PreventIncrement = new PreventIncrementConfiguration { OfMergedBranch = false }
        });
        root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Major, "oldest"));
        var resolver = Resolver(root);

        var result = resolver.GetEffectiveConfiguration(tip);

        result.Increment.ShouldBe(IncrementStrategy.Major);
        result.Label.ShouldBe("nearest");
        result.IsMainBranch.ShouldBeTrue();
        result.PreventIncrementOfMergedBranch.ShouldBeTrue();
        tip.Configuration.Label.ShouldBeNull();
        tip.Configuration.Increment.ShouldBe(IncrementStrategy.Inherit);
    }

    [Test]
    public void EquivalentInheritedConfigurationsReuseEffectiveConfiguration()
    {
        var root = Iteration("root", Branch(IncrementStrategy.Minor, "beta"));
        var firstMerge = root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Minor, "beta"));
        var secondMerge = root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Minor, "beta"));
        var inherited = new BranchConfiguration { Increment = IncrementStrategy.Inherit };
        var firstChild = Iteration("first", inherited, root, Commit(), firstMerge.Value);
        var secondChild = Iteration("second", inherited, root, Commit(), secondMerge.Value);
        var firstTip = firstChild.CreateCommit(Commit(), firstChild.BranchName, inherited);
        var secondTip = secondChild.CreateCommit(Commit(), secondChild.BranchName, inherited);
        firstMerge.AddChildIteration(firstChild);
        secondMerge.AddChildIteration(secondChild);
        var resolver = Resolver(root);

        var first = resolver.GetEffectiveConfiguration(firstTip);
        var second = resolver.GetEffectiveConfiguration(secondTip);

        first.Increment.ShouldBe(IncrementStrategy.Minor);
        first.Label.ShouldBe("beta");
        second.ShouldBeSameAs(first);
        resolver.GetEffectiveConfiguration(secondChild).ShouldBeSameAs(first);
        inherited.Increment.ShouldBe(IncrementStrategy.Inherit);
        inherited.Label.ShouldBeNull();
    }

    [Test]
    public void CommitInheritanceReusesCachedPredecessorWithoutReapplyingSettings()
    {
        var inherited = new CountingBranchConfiguration { Increment = IncrementStrategy.Inherit, Label = "nearest" };
        var root = Iteration("root", inherited);
        var tip = root.CreateCommit(Commit(), root.BranchName, inherited);
        var predecessor = root.CreateCommit(Commit(), root.BranchName, inherited);
        root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Major, "oldest"));
        var resolver = Resolver(root);
        var cached = resolver.GetEffectiveConfiguration(predecessor);
        inherited.InheritanceCount.ShouldBe(1);

        var result = resolver.GetEffectiveConfiguration(tip);

        result.ShouldBeSameAs(cached);
        result.Increment.ShouldBe(IncrementStrategy.Major);
        result.Label.ShouldBe("nearest");
        inherited.InheritanceCount.ShouldBe(1);
    }

    [Test]
    public void SharedInheritedSettingsKeepReceivingConfigurationsSeparate()
    {
        var receiver = Branch(IncrementStrategy.Minor, "beta");
        var root = Iteration("root", receiver);
        var firstMerge = root.CreateCommit(Commit(), root.BranchName, receiver);
        var secondMerge = root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Major, "alpha"));
        var inherited = new BranchConfiguration { Increment = IncrementStrategy.Inherit };
        var firstChild = Iteration("first", inherited, root, Commit(), firstMerge.Value);
        var secondChild = Iteration("second", inherited, root, Commit(), secondMerge.Value);
        var firstTip = firstChild.CreateCommit(Commit(), firstChild.BranchName, inherited);
        var secondTip = secondChild.CreateCommit(Commit(), secondChild.BranchName, inherited);
        firstMerge.AddChildIteration(firstChild);
        secondMerge.AddChildIteration(secondChild);
        var resolver = Resolver(root);

        var first = resolver.GetEffectiveConfiguration(firstTip);
        var second = resolver.GetEffectiveConfiguration(secondTip);

        first.Increment.ShouldBe(IncrementStrategy.Minor);
        first.Label.ShouldBe("beta");
        second.Increment.ShouldBe(IncrementStrategy.Major);
        second.Label.ShouldBe("alpha");
        second.ShouldNotBeSameAs(first);
        resolver.GetEffectiveConfiguration(firstTip).ShouldBeSameAs(first);
        resolver.GetEffectiveConfiguration(secondTip).ShouldBeSameAs(second);
    }

    [Test]
    public void ConfigurationPreEnrichmentRefreshesCurrentIterationAndChildSettings()
    {
        var branch = Branch(IncrementStrategy.Minor, "beta");
        var root = Iteration("root", branch);
        var merge = root.CreateCommit(Commit(), root.BranchName, branch);
        var child = Iteration("child", Branch(IncrementStrategy.Patch, "source"), root, Commit(), merge.Value);
        merge.AddChildIteration(child);
        var ordinary = root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Major, "alpha"));
        var context = MainlineTestCalculation.Context(root, GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(), merge.Value);
        var enricher = new ConfigurationPreEnricher();

        enricher.Enrich(root, merge, context);

        context.EffectiveConfiguration.Increment.ShouldBe(IncrementStrategy.Minor);
        context.IterationConfiguration.Increment.ShouldBe(IncrementStrategy.Minor);
        context.ChildConfiguration.ShouldNotBeNull().Increment.ShouldBe(IncrementStrategy.Patch);
        var nested = context.CreateChildContext();
        nested.EffectiveConfiguration.ShouldBeNull();
        nested.IterationConfiguration.ShouldBeNull();
        nested.ChildConfiguration.ShouldBeNull();

        enricher.Enrich(root, ordinary, context);

        context.EffectiveConfiguration.Increment.ShouldBe(IncrementStrategy.Major);
        context.EffectiveConfiguration.Label.ShouldBe("alpha");
        context.IterationConfiguration.Increment.ShouldBe(IncrementStrategy.Minor);
        context.ChildConfiguration.ShouldBeNull();
    }

    private sealed record CountingBranchConfiguration : BranchConfiguration
    {
        public int InheritanceCount { get; private set; }

        public override IBranchConfiguration Inherit(IBranchConfiguration configuration)
        {
            InheritanceCount++;
            return base.Inherit(configuration);
        }

        public override IBranchConfiguration Inherit(EffectiveConfiguration configuration)
        {
            InheritanceCount++;
            return base.Inherit(configuration);
        }
    }

    private static MainlineConfigurationResolver Resolver(MainlineIteration root)
        => new(GitFlowConfigurationBuilder.New.Build(), new MainlineCommitResolver(root));

    private static MainlineIteration Iteration(string id, IBranchConfiguration branch,
        MainlineIteration? parent = null, ICommit? branchedFrom = null, ICommit? branchedTo = null)
        => new(id, new($"refs/heads/{id}"), branch, parent, branchedFrom, branchedTo);

    private static BranchConfiguration Branch(IncrementStrategy increment, string label, bool mainline = true)
        => new() { Increment = increment, Label = label, IsMainBranch = mainline };

    private static ICommit Commit()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        return commit;
    }
}
