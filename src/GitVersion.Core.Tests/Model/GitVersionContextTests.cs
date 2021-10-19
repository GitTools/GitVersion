using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

public class GitVersionContextTests : TestBase
{
    [Test]
    [Theory]
    public void CanInheritVersioningMode(VersioningMode mode)
    {
        using var fixture = new EmptyRepositoryFixture();

        var config = new ConfigurationBuilder()
            .Add(new Config { VersioningMode = mode })
            .Build();

        const string branchName = MainBranch;

        var mockCommit = GitToolsTestingExtensions.CreateMockCommit();
        var mockBranch = GitToolsTestingExtensions.CreateMockBranch(branchName, mockCommit);

        var branches = Substitute.For<IBranchCollection>();
        branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)new[] { mockBranch }).GetEnumerator());

        var mockRepository = Substitute.For<IGitRepository>();
        mockRepository.Head.Returns(mockBranch);
        mockRepository.Branches.Returns(branches);
        mockRepository.Commits.Returns(mockBranch.Commits);

        var context = GetGitVersionContext(fixture.RepositoryPath, mockRepository, branchName, config);

        context.Configuration.VersioningMode.ShouldBe(mode);
    }

    [TestCase(IncrementStrategy.Inherit, IncrementStrategy.Patch)] // Since it inherits, the increment strategy of main is used => Patch
    [TestCase(IncrementStrategy.Patch, null)]
    [TestCase(IncrementStrategy.Major, null)]
    [TestCase(IncrementStrategy.Minor, null)]
    [TestCase(IncrementStrategy.None, null)]
    public void CanInheritIncrement(IncrementStrategy increment, IncrementStrategy? alternateExpected)
    {
        // Dummy branch name to make sure that no default config exists.
        const string dummyBranchName = "dummy";

        var config = new ConfigurationBuilder()
            .Add(new Config { Increment = increment })
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo(dummyBranchName);
        fixture.MakeACommit();

        var context = GetGitVersionContext(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), dummyBranchName, config);

        context.Configuration.Increment.ShouldBe(alternateExpected ?? increment);
    }

    [Test]
    public void UsesBranchSpecificConfigOverTopLevelDefaults()
    {
        using var fixture = new EmptyRepositoryFixture();

        const string branchName = "develop";
        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    {
                        branchName, new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDeployment,
                            Tag = "alpha"
                        }
                    }
                }
            })
            .Build();

        var main = GitToolsTestingExtensions.CreateMockBranch(MainBranch, GitToolsTestingExtensions.CreateMockCommit());
        var develop = GitToolsTestingExtensions.CreateMockBranch(branchName, GitToolsTestingExtensions.CreateMockCommit());

        var branches = Substitute.For<IBranchCollection>();
        branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)new[] { main, develop }).GetEnumerator());

        var mockRepository = Substitute.For<IGitRepository>();
        mockRepository.Head.Returns(develop);
        mockRepository.Branches.Returns(branches);
        mockRepository.Commits.Returns(develop.Commits);

        var context = GetGitVersionContext(fixture.RepositoryPath, mockRepository, branchName, config);

        context.Configuration.Tag.ShouldBe("alpha");
    }

    [Test]
    public void UsesFirstBranchConfigWhenMultipleMatch()
    {
        using var fixture = new EmptyRepositoryFixture();

        var branchConfig = new BranchConfig
        {
            VersioningMode = VersioningMode.Mainline,
            Increment = IncrementStrategy.None,
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            SourceBranches = new HashSet<string>()
        };
        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    { "release/latest", new BranchConfig(branchConfig) { Increment = IncrementStrategy.None, Regex = "release/latest" } },
                    { "release", new BranchConfig(branchConfig) { Increment = IncrementStrategy.Patch, Regex = "releases?[/-]" } }
                }
            })
            .Build();

        var releaseLatestBranch = GitToolsTestingExtensions.CreateMockBranch("release/latest", GitToolsTestingExtensions.CreateMockCommit());
        var releaseVersionBranch = GitToolsTestingExtensions.CreateMockBranch("release/1.0.0", GitToolsTestingExtensions.CreateMockCommit());

        var branches = Substitute.For<IBranchCollection>();
        branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)new[] { releaseLatestBranch, releaseVersionBranch }).GetEnumerator());

        var mockRepository = Substitute.For<IGitRepository>();
        mockRepository.Branches.Returns(branches);
        mockRepository.Head.Returns(releaseLatestBranch);
        mockRepository.Commits.Returns(releaseLatestBranch.Commits);

        var latestContext = GetGitVersionContext(fixture.RepositoryPath, mockRepository, releaseLatestBranch.Name.Canonical, config);
        latestContext.Configuration.Increment.ShouldBe(IncrementStrategy.None);

        mockRepository.Head.Returns(releaseVersionBranch);
        var versionContext = GetGitVersionContext(fixture.RepositoryPath, mockRepository, releaseVersionBranch.Name.Canonical, config);
        versionContext.Configuration.Increment.ShouldBe(IncrementStrategy.Patch);
    }

    [Test]
    public void CanFindParentBranchForInheritingIncrementStrategy()
    {
        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                Branches =
                {
                    { "develop", new BranchConfig { Increment = IncrementStrategy.Major } },
                    { "feature", new BranchConfig { Increment = IncrementStrategy.Inherit } }
                }
            })
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        var featureBranch = fixture.Repository.CreateBranch("feature/foo");
        Commands.Checkout(fixture.Repository, featureBranch);
        fixture.Repository.MakeACommit();

        var context = GetGitVersionContext(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), "develop", config);

        context.Configuration.Increment.ShouldBe(IncrementStrategy.Major);
    }

    private static GitVersionContext GetGitVersionContext(string workingDirectory, IGitRepository repository, string branch, Config config = null)
    {
        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = workingDirectory,
            RepositoryInfo = { TargetBranch = branch },
            ConfigInfo = { OverrideConfig = config }
        });

        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton(repository);
        });

        return sp.GetService<Lazy<GitVersionContext>>()?.Value;
    }
}
