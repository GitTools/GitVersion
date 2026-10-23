using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineIterationConstructionTests
{
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Inherit)]
    public void SourceBranchPointBeforeAnOlderMergedReleaseIsPreserved(IncrementStrategy featureIncrement)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/old");
        fixture.MakeACommit("Old feature +semver: major");
        fixture.BranchTo("support/2.x");
        fixture.MakeACommit("Source +semver: minor");
        fixture.Checkout("feature/old");
        fixture.MakeATaggedCommit("2.0.0");
        fixture.Checkout("main");
        fixture.MergeNoFF("feature/old");
        fixture.MakeATaggedCommit("2.0.1");
        fixture.MakeACommit();
        fixture.MergeNoFF("support/2.x");
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
            .WithBranch("feature", b => b.WithIncrement(featureIncrement))
            .WithBranch("main", b => b.WithPreventIncrementOfMergedBranch(false)).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.1.0");
    }

    [TestCase("feature/current", false, 2)]
    [TestCase("feature/current", true, 2)]
    [TestCase("support/2.x", false, 3)]
    [TestCase("support/2.x", true, 3)]
    public void ReleaseBoundaryDoesNotExpandOlderMerges(string sourceBranch, bool tagMerge, int expectedPatch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/old");
        fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.MergeNoFF("feature/old");
        var oldMerge = fixture.Repository.Head.Tip;
        var oldParents = oldMerge.Parents.ToArray();
        if (!tagMerge)
        {
            fixture.MakeACommit();
        }
        var release = fixture.Repository.Head.Tip.Sha;
        fixture.ApplyTag("2.0.0");
        fixture.BranchTo(sourceBranch);
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.MakeACommit();
        fixture.MergeNoFF(sourceBranch);
        var newMerge = fixture.Repository.Head.Tip;
        var newParents = newMerge.Parents.ToArray();
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
            .WithBranch("main", b => b.WithPreventIncrementOfMergedBranch(false)).Build();
        var provider = Substitute.For<IConfigurationProvider>();
        provider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(configuration);
        using var repository = fixture.Repository.ToGitRepository();
        using var services = (ServiceProvider)fixture.ConfigureServices((collection, _) =>
        {
            collection.AddSingleton(provider);
            collection.AddSingleton(Options.Create(new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath }));
            collection.AddSingleton(repository);
        });
        services.DiscoverRepository();
        var realStore = services.GetRequiredService<IRepositoryStore>();
        var store = Substitute.For<IRepositoryStore>();
        store.FindBranch(Arg.Any<ReferenceName>()).Returns(call => realStore.FindBranch(call.Arg<ReferenceName>()));
        store.FindCommitBranchesBranchedFrom(Arg.Any<IBranch>(), Arg.Any<IGitVersionConfiguration>(), Arg.Any<IBranch[]>())
            .Returns(call => realStore.FindCommitBranchesBranchedFrom(call.ArgAt<IBranch>(0), call.ArgAt<IGitVersionConfiguration>(1), call.ArgAt<IBranch[]>(2)));
        store.GetCommitLog(Arg.Any<ICommit>(), Arg.Any<ICommit>(), Arg.Any<IIgnoreConfiguration>())
            .Returns(call => realStore.GetCommitLog(call.ArgAt<ICommit>(0), call.ArgAt<ICommit>(1), call.ArgAt<IIgnoreConfiguration>(2)));
        var context = services.GetRequiredService<Lazy<GitVersionContext>>();
        var finder = services.GetRequiredService<IIncrementStrategyFinder>();
        var environment = services.GetRequiredService<IEnvironment>();
        var strategy = new MainlineVersionStrategy(context, store,
            services.GetRequiredService<ITaggedSemanticVersionService>(), finder, environment);

        var iteration = strategy.BuildIteration(configuration.GetEffectiveBranchConfiguration(repository.Head), out var label);
        var version = MainlineVersionStrategy.DetermineBaseVersionRecursive(iteration, label, finder,
            configuration, environment, context.Value.CurrentCommit);

        version.GetIncrementedVersion().Patch.ShouldBe(expectedPatch);
        version.GetIncrementedVersion().Minor.ShouldBe(0);
        version.GetIncrementedVersion().Major.ShouldBe(2);
        iteration.Commits.ShouldContain(commit => commit.Value.Sha == release);
        if (!tagMerge)
        {
            iteration.Commits.ShouldNotContain(commit => commit.Value.Sha == oldMerge.Sha);
        }
        store.Received().GetCommitLog(Arg.Is<ICommit>(commit => commit.Sha == newParents[0].Sha),
            Arg.Is<ICommit>(commit => commit.Sha == newParents[1].Sha), Arg.Any<IIgnoreConfiguration>());
        store.DidNotReceive().GetCommitLog(Arg.Is<ICommit>(commit => commit.Sha == oldParents[0].Sha),
            Arg.Is<ICommit>(commit => commit.Sha == oldParents[1].Sha), Arg.Any<IIgnoreConfiguration>());
    }

    [TestCase("", false)]
    [TestCase("", true)]
    [TestCase("beta", false)]
    [TestCase("beta", true)]
    public void IndependentSourceResumesOlderAncestryOnlyWhenItsOwnReleaseRequiresIt(string sourceLabel, bool tagSource)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/old");
        fixture.MakeACommit("Old feature +semver: major");
        fixture.Checkout("main");
        fixture.MergeNoFF("feature/old");
        var oldMerge = fixture.Repository.Head.Tip.Sha;
        fixture.BranchTo("support/2.x");
        fixture.MakeACommit("Source +semver: minor");
        if (tagSource)
        {
            fixture.ApplyTag(sourceLabel.Length == 0 ? "2.1.0" : $"2.1.0-{sourceLabel}.1");
        }
        fixture.Checkout("main");
        fixture.MakeATaggedCommit("2.0.0");
        fixture.MakeACommit();
        fixture.MergeNoFF("support/2.x");
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
            .WithBranch("main", b => b.WithPreventIncrementOfMergedBranch(false))
            .WithBranch("support", b => b.WithLabel(sourceLabel)).Build();
        var provider = Substitute.For<IConfigurationProvider>();
        provider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(configuration);
        using var repository = fixture.Repository.ToGitRepository();
        using var services = (ServiceProvider)fixture.ConfigureServices((collection, _) =>
        {
            collection.AddSingleton(provider);
            collection.AddSingleton(Options.Create(new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath }));
            collection.AddSingleton(repository);
        });
        services.DiscoverRepository();
        var context = services.GetRequiredService<Lazy<GitVersionContext>>();
        var finder = services.GetRequiredService<IIncrementStrategyFinder>();
        var environment = services.GetRequiredService<IEnvironment>();
        var strategy = new MainlineVersionStrategy(context, services.GetRequiredService<IRepositoryStore>(),
            services.GetRequiredService<ITaggedSemanticVersionService>(), finder, environment);

        var iteration = strategy.BuildIteration(configuration.GetEffectiveBranchConfiguration(repository.Head), out var label);
        iteration.Commits.ShouldNotContain(commit => commit.Value.Sha == oldMerge);
        var version = MainlineVersionStrategy.DetermineBaseVersionRecursive(iteration, label, finder,
            configuration, environment, context.Value.CurrentCommit).GetIncrementedVersion();

        version.Major.ShouldBe(2);
        version.Minor.ShouldBe(1);
        version.Patch.ShouldBe(0);
        iteration.Commits.Any(commit => commit.Value.Sha == oldMerge).ShouldBe(!tagSource);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void UnsupportedMergeIsRejectedOnlyWithinCalculationHistory(bool addReleaseTag, bool tagMerge)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        var first = fixture.Repository.Head.Tip;
        fixture.Checkout("main");
        fixture.BranchTo("support/2.x");
        fixture.MakeACommit();
        var second = fixture.Repository.Head.Tip;
        fixture.Checkout("main");
        fixture.MakeACommit();
        var receiver = fixture.Repository.Head.Tip;
        var merge = fixture.Repository.ObjectDatabase.CreateCommit(receiver.Author, receiver.Committer,
            "Merge branch 'support/1.x'", receiver.Tree, [receiver, first, second], false);
        fixture.Repository.Refs.UpdateTarget(fixture.Repository.Refs[fixture.Repository.Head.CanonicalName], merge.Sha);
        if (addReleaseTag)
        {
            if (!tagMerge)
            {
                fixture.MakeACommit();
            }
            fixture.ApplyTag("2.0.0");
        }
        fixture.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment).Build();

        if (addReleaseTag)
        {
            fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.0.1");
        }
        else
        {
            Should.Throw<NotSupportedException>(() => fixture.GetVersion(configuration));
        }
    }

    [Test]
    public void IgnoredSourceTipDoesNotTurnAnAncestorTagIntoTheReleasedTip()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeATaggedCommit("2.0.0");
        var ignoredTip = fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.MergeNoFF("support/1.x");
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [ignoredTip] }).Build();

        fixture.GetVersion(configuration).FullSemVer.ShouldBe("2.0.1");
    }

    [TestCase("support/1.x", 3)]
    [TestCase("feature/foo", 2)]
    public void MergedHistoryIsAttachedBeforeVersionCalculation(string sourceBranch, int expectedPatch)
    {
        using var fixture = new EmptyRepositoryFixture();
        var branchPoint = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo(sourceBranch);
        string[] sourceCommits = [fixture.MakeACommit(), fixture.MakeACommit(), fixture.MakeACommit()];
        fixture.Checkout("main");
        var receiverTip = fixture.MakeACommit();
        fixture.MergeNoFF(sourceBranch);
        var mergeSha = fixture.Repository.Head.Tip.Sha;
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithBranch("main", b => b.WithPreventIncrementOfMergedBranch(false)).Build();
        var provider = Substitute.For<IConfigurationProvider>();
        provider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(configuration);
        using var repository = fixture.Repository.ToGitRepository();
        using var services = (ServiceProvider)fixture.ConfigureServices((collection, _) =>
        {
            collection.AddSingleton(provider);
            collection.AddSingleton(Options.Create(new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath }));
            collection.AddSingleton(repository);
        });
        services.DiscoverRepository();
        var realFinder = services.GetRequiredService<IIncrementStrategyFinder>();
        var finder = Substitute.For<IIncrementStrategyFinder>();
        finder.GetMergedCommits(Arg.Any<ICommit>(), Arg.Any<int>(), Arg.Any<IIgnoreConfiguration>())
            .Returns(call => realFinder.GetMergedCommits(call.ArgAt<ICommit>(0), call.ArgAt<int>(1), call.ArgAt<IIgnoreConfiguration>(2)));
        finder.GetIncrementForcedByCommit(Arg.Any<ICommit>(), Arg.Any<IGitVersionConfiguration>())
            .Returns(call => realFinder.GetIncrementForcedByCommit(call.ArgAt<ICommit>(0), call.ArgAt<IGitVersionConfiguration>(1)));
        var context = services.GetRequiredService<Lazy<GitVersionContext>>();
        var environment = services.GetRequiredService<IEnvironment>();
        var strategy = new MainlineVersionStrategy(context, services.GetRequiredService<IRepositoryStore>(),
            services.GetRequiredService<ITaggedSemanticVersionService>(), finder, environment);

        var iteration = strategy.BuildIteration(configuration.GetEffectiveBranchConfiguration(repository.Head), out var label);

        finder.DidNotReceive().GetIncrementForcedByCommit(Arg.Any<ICommit>(), Arg.Any<IGitVersionConfiguration>());
        var merge = iteration.Commits.Single(commit => commit.Value.Sha == mergeSha);
        var child = merge.ChildIteration.ShouldNotBeNull();
        child.ParentIteration.ShouldBeSameAs(iteration);
        child.ParentCommit.ShouldBeSameAs(merge);
        child.Commits.Count.ShouldBe(3);
        child.BranchedFrom.ShouldBeSameAs(iteration.Commits.Single(commit => commit.Value.Sha == branchPoint));
        child.GetHistory().Count().ShouldBe(4);
        foreach (var sha in sourceCommits)
        {
            var commit = child.Commits.Single(commit => commit.Value.Sha == sha);
            commit.ParentIteration.ShouldBeSameAs(iteration);
            commit.ParentCommit.ShouldBeSameAs(merge);
        }
        iteration.Commits.ShouldContain(commit => commit.Value.Sha == receiverTip);

        var version = MainlineVersionStrategy.DetermineBaseVersionRecursive(iteration, label, finder,
            configuration, environment, context.Value.CurrentCommit);

        finder.Received().GetIncrementForcedByCommit(Arg.Any<ICommit>(), Arg.Any<IGitVersionConfiguration>());
        var calculated = version.GetIncrementedVersion();
        calculated.Major.ShouldBe(1);
        calculated.Minor.ShouldBe(0);
        calculated.Patch.ShouldBe(expectedPatch);
        version.BaseVersionSource.ShouldNotBeNull().Sha.ShouldBe(receiverTip);
    }
}
