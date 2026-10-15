using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
[NonParallelizable] // The tie scenario requires ordered timestamps from the shared VirtualTime clock.
public class DocumentationSamplesForVersionCalculation
{
    [TestCase("5.0.0", "5.0.0-1", "5.0.0", false, VersionField.None)]
    [TestCase("0.5.0", "1.0.1-1", "1.0.0", true, VersionField.Patch)]
    public void CompetingConfiguredAndTaggedVersions(string configuredVersion, string expectedVersion,
        string semanticSource, bool tagWins, VersionField increment)
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        var configuration = GitHubFlowConfigurationBuilder.New.WithNextVersion(configuredVersion).Build();

        fixture.AssertFullSemver(expectedVersion, configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSemVer.ShouldBe(semanticSource);
        version.SemVerSourceSha.ShouldBe(tagWins ? tagSha : null);
        version.SemVerSourceIncrement.ShouldBe(increment.ToString());
        version.CommitCountSourceSha.ShouldBe(tagSha);
        version.CommitCountSourceDistance.ShouldBe("1");
        fixture.SequenceDiagram.NoteOver($"next-version: {configuredVersion}", "main");
        Write(fixture, $"{nameof(CompetingConfiguredAndTaggedVersions)}_{(tagWins ? "TagWins" : "ConfigurationWins")}");
    }

    [TestCase(IncrementStrategy.Patch, "1.0.1-work.1+1")]
    [TestCase(IncrementStrategy.Minor, "1.1.0-work.1+1")]
    public void FeatureInheritsSourceIncrement(IncrementStrategy sourceIncrement, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/work");
        fixture.MakeACommit();
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithIncrement(sourceIncrement))
            .WithBranch("feature", builder => builder.WithIncrement(IncrementStrategy.Inherit)).Build();

        fixture.AssertFullSemver(expectedVersion, configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSha.ShouldBe(tagSha);
        version.SemVerSourceIncrement.ShouldBe(sourceIncrement.ToString());
        version.CommitCountSourceSha.ShouldBe(tagSha);
        version.CommitCountSourceDistance.ShouldBe("1");
        fixture.SequenceDiagram.NoteOver($"main increment: {sourceIncrement}; feature increment: Inherit", "feature/work");
        Write(fixture, $"{nameof(FeatureInheritsSourceIncrement)}_{sourceIncrement}");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EqualCandidatesUseLatestSource(bool reverseCandidateOrder)
    {
        using var fixture = new EmptyRepositoryFixture();
        var olderSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.SequenceDiagram.NoteOver("Controlled candidate A: 2.0.0 at this commit", "main");
        var newerSha = fixture.MakeACommit();
        fixture.SequenceDiagram.NoteOver("Controlled candidate B: 2.0.0 at this later commit", "main");
        fixture.MakeACommit();
        using var repository = fixture.Repository.ToGitRepository();
        var olderCommit = repository.Commits.Single(commit => commit.Sha == olderSha);
        var newerCommit = repository.Commits.Single(commit => commit.Sha == newerSha);
        newerCommit.When.ShouldBeGreaterThan(olderCommit.When);
        var strategy = Substitute.For<IVersionStrategy>();
        // Supply the same semantic version from two commits to isolate the tie rule.
        BaseVersion[] candidates = [
            new BaseVersion("Older candidate", new SemanticVersion(2, 0, 0), olderCommit),
            new BaseVersion("Newer candidate", new SemanticVersion(2, 0, 0), newerCommit)
        ];
        if (reverseCandidateOrder)
        {
            Array.Reverse(candidates);
        }
        strategy.GetBaseVersions(Arg.Any<EffectiveBranchConfiguration>()).Returns(candidates);
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithDeploymentMode(DeploymentMode.ManualDeployment)).Build();
        var provider = Substitute.For<IConfigurationProvider>();
        provider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(configuration);
        var options = Options.Create(new GitVersionOptions { WorkingDirectory = fixture.Repository.Info.WorkingDirectory });
        using var services = (ServiceProvider)fixture.ConfigureServices((collection, _) =>
        {
            collection.AddSingleton(provider);
            collection.AddSingleton(options);
            collection.AddSingleton(repository);
            collection.RemoveAll<IVersionStrategy>();
            collection.AddSingleton(strategy);
        });
        services.DiscoverRepository();

        var version = services.GetRequiredService<INextVersionCalculator>().FindVersion();
        version.ToString("f").ShouldBe("2.0.0+1");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(newerSha);
        version.BuildMetaData.SemVerSourceSemVer.ShouldNotBeNull().ToString().ShouldBe("2.0.0");
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.None);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(newerSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(1);
        fixture.SequenceDiagram.NoteOver("Equal candidates: 2.0.0 at tag commit and next commit; latest source wins", "main");
        fixture.SequenceDiagram.NoteOver("2.0.0+1", "main", color: "#D3D3D3");
        Write(fixture, nameof(EqualCandidatesUseLatestSource));
    }

    [Test]
    public void MergeMessageAndCountingSourceDiffer()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("release/2.0.0");
        fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.MergeNoFF("release/2.0.0");
        var mergeSha = fixture.Repository.Head.Tip.Sha;
        fixture.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New.Build();

        fixture.AssertFullSemver("2.0.0-3", configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSemVer.ShouldBe("2.0.0");
        version.SemVerSourceSha.ShouldBe(mergeSha);
        version.SemVerSourceIncrement.ShouldBe("None");
        version.CommitCountSourceSha.ShouldBe(tagSha);
        version.CommitCountSourceDistance.ShouldBe("3");
        Write(fixture, nameof(MergeMessageAndCountingSourceDiffer));
    }

    [TestCase(DeploymentMode.ManualDeployment, "1.0.1-beta.1+2")]
    [TestCase(DeploymentMode.ContinuousDelivery, "1.0.1-beta.2")]
    [TestCase(DeploymentMode.ContinuousDeployment, "1.0.1")]
    public void DeploymentModesProcessSameHistory(DeploymentMode mode, string expectedVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.MakeACommit();
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithLabel("beta").WithDeploymentMode(mode)).Build();

        fixture.AssertFullSemver(expectedVersion, configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSemVer.ShouldBe("1.0.0");
        version.SemVerSourceSha.ShouldBe(tagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(tagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
        Write(fixture, $"{nameof(DeploymentModesProcessSameHistory)}_{mode}");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MainlineTaggedBranchesMerge(bool mergeMainIntoSupport)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeATaggedCommit("1.0.5");
        fixture.Checkout("main");
        var mainTagSha = fixture.MakeATaggedCommit("2.0.0");
        if (mergeMainIntoSupport)
        {
            fixture.Checkout("support/1.x");
            fixture.MergeNoFF("main");
        }
        else
        {
            fixture.MergeNoFF("support/1.x");
        }
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery).Build();

        fixture.AssertFullSemver("2.0.1-2", configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSemVer.ShouldBe("2.0.0");
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("2");
        Write(fixture, $"{nameof(MainlineTaggedBranchesMerge)}_{(mergeMainIntoSupport ? "MainIntoSupport" : "SupportIntoMain")}");
    }

    [Test]
    public void MainlineSupportPreservesOrderedIncrements()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeATaggedCommit("1.0.5");
        fixture.MakeACommit("New API +semver: minor");
        fixture.MakeACommit();
        fixture.Checkout("main");
        var mainTagSha = fixture.MakeATaggedCommit("2.0.0");
        fixture.MergeNoFF("support/1.x");
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery).Build();

        fixture.AssertFullSemver("2.1.1-4", configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSha.ShouldBe(mainTagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(mainTagSha);
        version.CommitCountSourceDistance.ShouldBe("4");
        Write(fixture, nameof(MainlineSupportPreservesOrderedIncrements));
    }

    [Test]
    public void MainlineComparesIndependentProgress()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout("main");
        var mainTip = fixture.MakeACommit();
        fixture.MergeNoFF("support/1.x");
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery).Build();

        fixture.AssertFullSemver("1.0.3-4", configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSha.ShouldBe(tagSha);
        version.SemVerSourceIncrement.ShouldBe("Patch");
        version.CommitCountSourceSha.ShouldBe(mainTip);
        version.CommitCountSourceDistance.ShouldBe("4");
        Write(fixture, nameof(MainlineComparesIndependentProgress));
    }

    [Test]
    public void MainlineRoundTripDoesNotReplayMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("support/1.x");
        fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.MakeACommit("New API +semver: minor");
        var mainTip = fixture.Repository.Head.Tip.Sha;
        fixture.Checkout("support/1.x");
        fixture.MergeNoFF("main");
        fixture.Checkout("main");
        fixture.MergeNoFF("support/1.x");
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategies(VersionStrategies.Mainline)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment).Build();

        fixture.AssertFullSemver("1.1.1", configuration);
        var version = fixture.GetVersion(configuration);
        version.SemVerSourceSha.ShouldBe(tagSha);
        version.SemVerSourceIncrement.ShouldBe("None");
        version.CommitCountSourceSha.ShouldBe(mainTip);
        version.CommitCountSourceDistance.ShouldBe("3");
        Write(fixture, nameof(MainlineRoundTripDoesNotReplayMain));
    }

    private static void Write(EmptyRepositoryFixture fixture, string name)
        => DocumentationDiagramWriter.Write(fixture.SequenceDiagram,
            $"{nameof(DocumentationSamplesForVersionCalculation)}_{name}", true);
}
