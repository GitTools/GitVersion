using GitVersion.Configuration;
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
        resolver.GetIncrementForcedByBranch(local).ShouldBe(VersionField.Minor);
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
        resolver.GetIncrementForcedByBranch(tip).ShouldBe(VersionField.Minor);
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

    [TestCase(null)]
    [TestCase("")]
    public void OwnConfigurationKeepsRecordedLabelsSeparateFromInheritedSettings(string? label)
    {
        var recorded = new BranchConfiguration { Increment = IncrementStrategy.Inherit, Label = label };
        var root = Iteration("root", recorded);
        var tip = root.CreateCommit(Commit(), root.BranchName, recorded);
        root.CreateCommit(Commit(), root.BranchName, Branch(IncrementStrategy.Minor, "beta"));
        var configuration = GitFlowConfigurationBuilder.New.WithLabel("global").Build();
        var resolver = new MainlineConfigurationResolver(configuration, new MainlineBranchPointResolver(root));

        var own = resolver.GetOwnConfiguration(tip);
        var effective = resolver.GetEffectiveConfiguration(tip);

        own.Increment.ShouldBe(IncrementStrategy.Inherit);
        own.Label.ShouldBe(label ?? "global");
        effective.Increment.ShouldBe(IncrementStrategy.Minor);
        effective.Label.ShouldBe(label ?? "beta");
        tip.HasExplicitIncrement.ShouldBeFalse();
        tip.Configuration.Label.ShouldBe(label);
    }

    [Test]
    public void ConfigurationCachesAreIsolatedBetweenCalculations()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var root = Iteration("root", branch);
        var commit = root.CreateCommit(Commit(), root.BranchName, branch);
        var branchPoints = new MainlineBranchPointResolver(root);
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

        Resolver(root).IsMergedMainline(merge).ShouldBe(expected);
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
        resolver.IsMergedMainline(merge).ShouldBeTrue();
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

        resolver.IsMergedMainline(commit).ShouldBeFalse();
        resolver.GetIncrementForcedByBranch(commit).ShouldBe(VersionField.Major);
    }

    [Test]
    public void ChildContextsShareConfigurationResolutionButKeepCalculationStateIndependent()
    {
        var branch = Branch(IncrementStrategy.Minor, "beta");
        var root = Iteration("root", branch);
        var commit = root.CreateCommit(Commit(), root.BranchName, branch);
        var configuration = GitFlowConfigurationBuilder.New.Build();
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), commit.Value)
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
        var resolver = context.GetConfigurationResolver(root);
        var effective = context.GetEffectiveConfiguration(commit);

        var child = context.CreateChildContext();

        child.GetConfigurationResolver(root).ShouldBeSameAs(resolver);
        child.BranchPointResolver.ShouldBeSameAs(context.BranchPointResolver);
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

    private static MainlineConfigurationResolver Resolver(MainlineIteration root)
        => new(GitFlowConfigurationBuilder.New.Build(), new MainlineBranchPointResolver(root));

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
