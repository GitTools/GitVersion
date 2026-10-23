using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineIterationConstructionTests
{
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
