using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
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

        var strategy = GetVersionStrategy(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), branchName);
        var baseVersion = strategy.GetVersions().Single();

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

        var strategy = GetVersionStrategy(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), branchName);
        var baseVersions = strategy.GetVersions();

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
            .Add(new Config { Branches = { { "support", new BranchConfig { IsReleaseBranch = true } } } })
            .Build();

        var strategy = GetVersionStrategy(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), branchName, config);

        var baseVersion = strategy.GetVersions().Single();

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

        var strategy = GetVersionStrategy(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), branchName);
        var baseVersion = strategy.GetVersions().Single();

        baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
    }

    private static IVersionStrategy GetVersionStrategy(string workingDirectory, IGitRepository repository, string branch, Config config = null)
    {
        var sp = BuildServiceProvider(workingDirectory, repository, branch, config);
        return sp.GetServiceForType<IVersionStrategy, VersionInBranchNameVersionStrategy>();
    }
}
