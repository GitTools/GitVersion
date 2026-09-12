using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GitVersion.Tests;

[TestFixture]
public class VersionSourceTests : TestBase
{
    [TestCase(1)]
    [TestCase(2)]
    public void ConfiguredVersionKeepsExternalSemanticSource(int commitsAfterTag)
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        for (var index = 0; index < commitsAfterTag; index++)
        {
            fixture.MakeACommit();
        }
        var configuration = GitHubFlowConfigurationBuilder.New.WithNextVersion("5.0.0").Build();

        var version = FindVersion(fixture, configuration);

        version.ToString("f").ShouldBe($"5.0.0-{commitsAfterTag}");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("5.0.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBeNull();
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.None);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(commitsAfterTag);
        version.BuildMetaData.VersionSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.VersionSourceDistance.ShouldBe(commitsAfterTag);
    }

    [Test]
    public void BranchNameKeepsExternalSemanticSource()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("release/5.0.0");
        fixture.MakeACommit();

        var version = FindVersion(fixture, GitFlowConfigurationBuilder.New.Build());

        version.ToString("f").ShouldBe("5.0.0-beta.1+1");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("5.0.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBeNull();
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.None);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(1);
    }

    [TestCase("release/some_release", true)]
    [TestCase("release/1.3.0", false)]
    public void TrackedReleaseKeepsSemanticOriginSeparateFromMergeBase(string releaseBranch, bool usesTag)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        var mergeBaseSha = fixture.MakeACommit();
        fixture.BranchTo(releaseBranch);
        var tagSha = fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        fixture.MakeACommit();

        var version = FindVersion(fixture, GitFlowConfigurationBuilder.New.Build());

        version.ToString("f").ShouldBe("1.4.0-alpha.1");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("1.3.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(usesTag ? tagSha : null);
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.Minor);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(mergeBaseSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(1);
        version.BuildMetaData.VersionSourceSha.ShouldBe(mergeBaseSha);
    }

    [Test]
    public void ExternalOnlySourceCountsReachableHistory()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        var configuration = GitHubFlowConfigurationBuilder.New.WithNextVersion("5.0.0").Build();

        var version = FindVersion(fixture, configuration);

        version.ToString("f").ShouldBe("5.0.0-2");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("5.0.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBeNull();
        version.BuildMetaData.CommitCountSourceSha.ShouldBeNull();
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(2);
    }

    [Test]
    public void OrdinaryTagRetainsSelectedIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        var version = FindVersion(fixture, GitHubFlowConfigurationBuilder.New.Build());

        version.ToString("f").ShouldBe("1.0.1-1");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("1.0.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.Patch);
        version.BuildMetaData.VersionSourceIncrement.ShouldBe(VersionField.None);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TaggedHeadUsesTagForBothSources(bool inheritedIncrement)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        if (inheritedIncrement)
        {
            fixture.BranchTo("feature/inherited");
        }
        var tagSha = fixture.MakeATaggedCommit("2.3.4");

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("feature", builder => builder.WithIncrement(IncrementStrategy.Inherit)
                .WithPreventIncrementWhenCurrentCommitTagged(true)).Build();
        var version = FindVersion(fixture, configuration);

        version.ToString("f").ShouldBe("2.3.4");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("2.3.4");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.None);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(0);
    }

    [TestCase(DeploymentMode.ManualDeployment, "1.0.1-beta.1+2")]
    [TestCase(DeploymentMode.ContinuousDelivery, "1.0.1-beta.2")]
    [TestCase(DeploymentMode.ContinuousDeployment, "1.0.1")]
    public void DeploymentModesPreserveSourceMetadata(DeploymentMode mode, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.MakeACommit();
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithLabel("beta").WithDeploymentMode(mode)).Build();

        var version = FindVersion(fixture, configuration);

        version.ToString("f").ShouldBe(fullSemVer);
        version.BuildMetaData.SemVerSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("1.0.0");
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.Patch);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(2);
    }

    [Test]
    public void IgnoredCommitInMergeGraphIsExcludedFromDistance()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/counting");
        var ignoredSha = fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.MergeNoFF("feature/counting");
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithNextVersion("5.0.0")
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [ignoredSha] }).Build();

        var version = FindVersion(fixture, configuration);

        // The retained side-branch commit, main commit and merge all count.
        version.ToString("f").ShouldBe("5.0.0-3");
        version.BuildMetaData.SemVerSourceSha.ShouldBeNull();
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("5.0.0");
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(3);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReleaseBranchPresenceDoesNotChangeLatestSource(bool deleteReleaseBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("0.1.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/0.2.0");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("release/0.2.0");
        fixture.ApplyTag("0.2.0");
        var releaseTagSha = fixture.Repository.Head.Tip.Sha;
        if (deleteReleaseBranch)
        {
            fixture.Repository.Branches.Remove("release/0.2.0");
        }
        fixture.Checkout("develop");
        fixture.MergeNoFF(MainBranch);
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("develop", builder => builder.WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithTrackMergeMessage(false)).Build();

        var version = FindVersion(fixture, configuration);

        version.ToString("f").ShouldBe("0.3.0-alpha.1+1");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("0.2.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(releaseTagSha);
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.Minor);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(releaseTagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(1);
    }

    [Test]
    public void AlternativeTagFloorReplacesSemanticSourceWithoutResettingCount()
    {
        using var fixture = new EmptyRepositoryFixture();
        var countSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/work");
        var floorSha = fixture.MakeATaggedCommit("5.0.0-other.1");

        var version = FindVersion(fixture, GitHubFlowConfigurationBuilder.New.Build());

        version.ToString("f").ShouldBe("5.0.0-work.1+1");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("5.0.0-other.1");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(floorSha);
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.None);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(countSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(1);
        version.BuildMetaData.VersionSourceSemVer?.ToString().ShouldBe("1.0.0");
    }

    [Test]
    public void MainlineFoldKeepsExternalOriginWhenOperatorChangesCountAnchor()
    {
        var countCommit = GitRepositoryTestingExtensions.CreateMockCommit();
        var nextCountCommit = GitRepositoryTestingExtensions.CreateMockCommit();
        var version = new BaseVersion("Configured version", new SemanticVersion(5, 0, 0))
        {
            Operator = new BaseVersionOperator { Increment = VersionField.Patch, BaseVersionSource = countCommit }
        };

        var folded = version.Apply(new BaseVersionOperator { Increment = VersionField.Minor, ForceIncrement = true, BaseVersionSource = nextCountCommit });

        folded.GetIncrementedVersion().ToString().ShouldBe("5.1.0-1");
        folded.BaseVersionSource.ShouldBe(nextCountCommit);
        var source = folded.GetSemVerSource();
        source.Commit.ShouldBeNull();
        source.Version.ToString().ShouldBe("5.0.1-1");
        source.Increment.ShouldBe(VersionField.Minor);
    }

    [TestCase(5, "5.0.0", VersionField.None)]
    [TestCase(0, "1.0.0", VersionField.Patch)]
    public void OperatorAlternativeFloorUsesSupplyingSourceOnlyWhenItWins(int alternativeMajor, string sourceVersion, VersionField increment)
    {
        var originalCommit = GitRepositoryTestingExtensions.CreateMockCommit();
        var alternativeCommit = GitRepositoryTestingExtensions.CreateMockCommit();
        var alternative = new SemanticVersion(alternativeMajor, 0, 0);
        var version = new BaseVersion("Original tag", new SemanticVersion(1, 0, 0), originalCommit)
        {
            Operator = new BaseVersionOperator
            {
                Increment = VersionField.Patch,
                AlternativeSemanticVersion = alternative,
                AlternativeSemVerSource = new SemanticVersionSource("Alternative tag", alternative, alternativeCommit, VersionField.None)
            }
        };

        var source = version.GetSemVerSource();

        source.Version.ToString().ShouldBe(sourceVersion);
        source.Commit.ShouldBe(alternativeMajor == 5 ? alternativeCommit : originalCommit);
        source.Increment.ShouldBe(increment);
        version.GetIncrementedVersion().ToString().ShouldBe(alternativeMajor == 5 ? "5.0.0-1" : "1.0.1-1");
        version.BaseVersionSource.ShouldBe(originalCommit);
    }

    [Test]
    public void MainlineRepeatedIncrementsKeepTagOrigin()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        var previousSha = fixture.MakeACommit();
        fixture.MakeACommit();

        var version = FindVersion(fixture, TrunkBasedConfigurationBuilder.New.Build());

        version.ToString("f").ShouldBe("1.0.2");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("1.0.1-1");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.Patch);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(previousSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(1);
    }

    [Test]
    public void MainlineMergeKeepsChildSemanticOrigin()
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/work");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/work");

        var version = FindVersion(fixture, TrunkBasedConfigurationBuilder.New.Build());

        version.ToString("f").ShouldBe("1.1.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("1.0.0");
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.Minor);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(3);
    }

    [TestCase("2.0.0", "2.0.0-3")]
    [TestCase("1.0.1", "1.0.1-3")]
    public void MergeMessageKeepsSupplyingCommitSeparateFromOlderTag(string releaseVersion, string fullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var tagSha = fixture.MakeATaggedCommit("1.0.0");
        var releaseBranch = $"release/{releaseVersion}";
        fixture.BranchTo(releaseBranch);
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(releaseBranch);
        var mergeSha = fixture.Repository.Head.Tip.Sha;
        fixture.MakeACommit();

        var version = FindVersion(fixture, GitFlowConfigurationBuilder.New.Build());

        version.ToString("f").ShouldBe(fullSemVer);
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe(releaseVersion);
        version.BuildMetaData.SemVerSourceSha.ShouldBe(mergeSha);
        version.BuildMetaData.SemVerSourceIncrement.ShouldBe(VersionField.None);
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(tagSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(3);
    }

    [Test]
    public void StableWinnerExcludesPreReleaseCandidateFromCountingSelection()
    {
        using var fixture = new EmptyRepositoryFixture();
        var stableSha = fixture.MakeATaggedCommit("1.0.0");
        var preReleaseSha = fixture.MakeATaggedCommit("4.0.0-alpha.1");
        fixture.MakeACommit();
        using var repository = fixture.Repository.ToGitRepository();
        var stableCommit = repository.Commits.Single(commit => commit.Sha == stableSha);
        var preReleaseCommit = repository.Commits.Single(commit => commit.Sha == preReleaseSha);
        var strategy = Substitute.For<IVersionStrategy>();
        strategy.GetBaseVersions(Arg.Any<EffectiveBranchConfiguration>()).Returns([
            new BaseVersion("External stable version", new SemanticVersion(5, 0, 0)),
            new BaseVersion("Pre-release tag", SemanticVersion.Parse("4.0.0-alpha.1", null), preReleaseCommit),
            new BaseVersion("Stable tag", new SemanticVersion(1, 0, 0), stableCommit)
        ]);

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithDeploymentMode(DeploymentMode.ContinuousDeployment)).Build();
        var version = FindVersion(fixture, configuration, strategy, repository);

        version.ToString("f").ShouldBe("5.0.0");
        version.BuildMetaData.SemVerSourceSemVer?.ToString().ShouldBe("5.0.0");
        version.BuildMetaData.SemVerSourceSha.ShouldBeNull();
        version.BuildMetaData.CommitCountSourceSha.ShouldBe(stableSha);
        version.BuildMetaData.CommitCountSourceDistance.ShouldBe(2);
    }

    private static SemanticVersion FindVersion(EmptyRepositoryFixture fixture, IGitVersionConfiguration configuration,
        IVersionStrategy? strategy = null, IGitRepository? repository = null)
    {
        var provider = Substitute.For<IConfigurationProvider>();
        provider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(configuration);
        var options = Options.Create(new GitVersionOptions { WorkingDirectory = fixture.Repository.Info.WorkingDirectory });
        using var services = (ServiceProvider)fixture.ConfigureServices((collection, _) =>
        {
            collection.AddSingleton(provider);
            collection.AddSingleton(options);
            if (repository is not null)
            {
                collection.AddSingleton(repository);
            }
            if (strategy is not null)
            {
                collection.RemoveAll<IVersionStrategy>();
                collection.AddSingleton(strategy);
            }
        });
        services.DiscoverRepository();
        return services.GetRequiredService<INextVersionCalculator>().FindVersion();
    }

    [Test]
    public void VersionSourceSha()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture.Repository.ToGitRepository());

        var semanticVersion = nextVersionCalculator.FindVersion();

        semanticVersion.BuildMetaData.VersionSourceSha.ShouldBeNull();
        semanticVersion.BuildMetaData.VersionSourceDistance.ShouldBe(3);
        semanticVersion.BuildMetaData.SemVerSourceSha.ShouldBeNull();
        semanticVersion.BuildMetaData.CommitCountSourceSha.ShouldBeNull();
        semanticVersion.BuildMetaData.CommitCountSourceDistance.ShouldBe(3);
    }

    [Test]
    public void VersionSourceShaOneCommit()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture.Repository.ToGitRepository());

        var semanticVersion = nextVersionCalculator.FindVersion();

        semanticVersion.BuildMetaData.VersionSourceSha.ShouldBeNull();
        semanticVersion.BuildMetaData.VersionSourceDistance.ShouldBe(1);
    }

    [Test]
    public void VersionSourceShaUsingTag()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("develop");
        var secondCommitSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture.Repository.ToGitRepository());

        var semanticVersion = nextVersionCalculator.FindVersion();

        semanticVersion.BuildMetaData.VersionSourceSha.ShouldBe(secondCommitSha);
        semanticVersion.BuildMetaData.VersionSourceDistance.ShouldBe(1);
    }

    private static INextVersionCalculator GetNextVersionCalculator(IGitRepository repository)
    {
        var serviceProvider = BuildServiceProvider(repository);
        return serviceProvider.GetRequiredService<INextVersionCalculator>();
    }
}
