using GitTools.Testing;
using GitVersion.Configurations;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Model.Configurations;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.VersionCalculation.Strategies;

[TestFixture]
public class VersionInBranchNameBaseVersionStrategyTests : TestBase
{
    [TestCase("release-2.0.0", "2.0.0")]
    [TestCase("release/3.0.0", "3.0.0")]
    public void CanTakeVersionFromNameOfReleaseBranch(string branchName, string expectedBaseVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch(branchName);

        var gitRepository = fixture.Repository.ToGitRepository();
        var strategy = GetVersionStrategy(fixture.RepositoryPath, gitRepository, branchName);
        var configuration = TestConfigurationBuilder.New.Build();
        var branchConfiguration = configuration.GetBranchConfiguration(branchName);
        var effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
        var baseVersion = strategy.GetBaseVersions(new(gitRepository.FindBranch(branchName)!, effectiveConfiguration)).Single();

        baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
    }

    [TestCase("hotfix-2.0.0")]
    [TestCase("hotfix/2.0.0")]
    [TestCase("custom/JIRA-123")]
    [TestCase("hotfix/downgrade-to-gitversion-3.6.5-to-fix-miscalculated-version")]
    public void ShouldNotTakeVersionFromNameOfNonReleaseBranch(string branchName)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch(branchName);

        var gitRepository = fixture.Repository.ToGitRepository();
        var strategy = GetVersionStrategy(fixture.RepositoryPath, gitRepository, branchName);
        var configuration = TestConfigurationBuilder.New.Build();
        var branchConfiguration = configuration.GetBranchConfiguration(branchName);
        var effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
        var baseVersions = strategy.GetBaseVersions(new(gitRepository.FindBranch(branchName)!, effectiveConfiguration));

        baseVersions.ShouldBeEmpty();
    }

    [TestCase("support/lts-2.0.0", "2.0.0")]
    [TestCase("support-3.0.0-lts", "3.0.0")]
    public void CanTakeVersionFromNameOfConfiguredReleaseBranch(string branchName, string expectedBaseVersion)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch(branchName);

        var config = new ConfigurationBuilder()
            .Add(new Model.Configurations.Configuration { Branches = { { "support", new BranchConfig { IsReleaseBranch = true } } } })
            .Build();

        var gitRepository = fixture.Repository.ToGitRepository();
        var strategy = GetVersionStrategy(fixture.RepositoryPath, gitRepository, branchName, config);

        var configuration = TestConfigurationBuilder.New.Build();
        var branchConfiguration = configuration.GetBranchConfiguration(branchName);
        var effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
        var baseVersion = strategy.GetBaseVersions(new(gitRepository.FindBranch(branchName)!, effectiveConfiguration)).Single();

        baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
    }

    [TestCase("release-2.0.0", "2.0.0")]
    [TestCase("release/3.0.0", "3.0.0")]
    public void CanTakeVersionFromNameOfRemoteReleaseBranch(string branchName, string expectedBaseVersion)
    {
        using var fixture = new RemoteRepositoryFixture();
        var branch = fixture.Repository.CreateBranch(branchName);
        Commands.Checkout(fixture.Repository, branch);
        fixture.MakeACommit();

        Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);
        fixture.LocalRepositoryFixture.Checkout($"origin/{branchName}");

        var gitRepository = fixture.Repository.ToGitRepository();
        var strategy = GetVersionStrategy(fixture.RepositoryPath, gitRepository, branchName);

        var configuration = TestConfigurationBuilder.New.Build();
        var branchConfiguration = configuration.GetBranchConfiguration(branchName);
        var effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
        var baseVersion = strategy.GetBaseVersions(new(gitRepository.FindBranch(branchName)!, effectiveConfiguration)).Single();

        baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
    }

    private static IVersionStrategy GetVersionStrategy(string workingDirectory, IGitRepository repository, string branch, Model.Configurations.Configuration? config = null)
    {
        var sp = BuildServiceProvider(workingDirectory, repository, branch, config);
        return sp.GetServiceForType<IVersionStrategy, VersionInBranchNameVersionStrategy>();
    }
}
