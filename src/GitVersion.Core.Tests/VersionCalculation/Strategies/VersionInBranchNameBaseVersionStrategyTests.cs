using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.VersionCalculation.Strategies;

[TestFixture]
public class VersionInBranchNameBaseVersionStrategyTests : TestBase
{
    [TestCase("release-2.0.0", "2.0.0")]
    [TestCase("release/3.0.0", "3.0.0")]
    public void CanTakeVersionFromNameOfReleaseBranch(string branchName, string expectedBaseVersion)
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.CreateBranch(branchName);

        var repository = fixture.Repository.ToGitRepository();

        var strategy = GetVersionStrategy(repository);
        var branch = repository.FindBranch(branchName);

        var configuration = GitFlowConfigurationBuilder.New.Build();
        var effectiveBranchConfiguration = configuration.GetEffectiveBranchConfiguration(branch);

        strategy.ShouldNotBeNull();
        var baseVersion = strategy.GetBaseVersions(effectiveBranchConfiguration).Single();

        baseVersion.GetSemanticVersion().ToString().ShouldBe(expectedBaseVersion);
    }

    [TestCase("origin/hotfix-2.0.0")]
    [TestCase("remotes/origin/hotfix-2.0.0")]
    [TestCase("origin/hotfix/2.0.0")]
    [TestCase("remotes/origin/hotfix/2.0.0")]
    [TestCase("custom/JIRA-123")]
    [TestCase("remotes/custom/JIRA-123")]
    [TestCase("hotfix/downgrade-to-gitversion-3.6.5-to-fix-miscalculated-version")]
    [TestCase("remotes/hotfix/downgrade-to-gitversion-3.6.5-to-fix-miscalculated-version")]
    public void ShouldNotTakeVersionFromNameOfNonReleaseBranch(string branchName)
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.CreateBranch(branchName);

        var repository = fixture.Repository.ToGitRepository();

        var strategy = GetVersionStrategy(repository);
        var branch = repository.FindBranch(branchName);

        var configuration = GitFlowConfigurationBuilder.New.Build();
        var effectiveBranchConfiguration = configuration.GetEffectiveBranchConfiguration(branch);

        strategy.ShouldNotBeNull();
        var baseVersions = strategy.GetBaseVersions(effectiveBranchConfiguration);

        baseVersions.ShouldBeEmpty();
    }

    [TestCase("release-2.0.0", "2.0.0")]
    [TestCase("release/3.0.0", "3.0.0")]
    [TestCase("support/2.0.0-lts", "2.0.0")]
    [TestCase("support-3.0.0-lts", "3.0.0")]
    [TestCase("hotfix/2.0.0", "2.0.0")]
    [TestCase("hotfix-3.0.0", "3.0.0")]
    [TestCase("hotfix/2.0.0-lts", "2.0.0")]
    [TestCase("hotfix-3.0.0-lts", "3.0.0")]
    public void CanTakeVersionFromNameOfConfiguredReleaseBranch(string branchName, string expectedBaseVersion)
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.CreateBranch(branchName);

        var repository = fixture.Repository.ToGitRepository();

        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("support", builder => builder.WithIsReleaseBranch(true))
            .Build();
        ConfigurationHelper configurationHelper = new(configuration);

        var strategy = GetVersionStrategy(repository, null, configurationHelper.Dictionary);
        var branch = repository.FindBranch(branchName);

        var effectiveBranchConfiguration = configuration.GetEffectiveBranchConfiguration(branch);

        strategy.ShouldNotBeNull();
        var baseVersion = strategy.GetBaseVersions(effectiveBranchConfiguration).Single();

        baseVersion.GetSemanticVersion().ToString().ShouldBe(expectedBaseVersion);
    }

    [TestCase("origin", "release-2.0.0", "2.0.0")]
    [TestCase("origin", "release/3.0.0", "3.0.0")]
    public void CanTakeVersionFromNameOfRemoteReleaseBranch(string origin, string branchName, string expectedBaseVersion)
    {
        using var fixture = new RemoteRepositoryFixture();

        fixture.CreateBranch(branchName);
        if (origin != "origin") fixture.LocalRepositoryFixture.Repository.Network.Remotes.Rename("origin", origin);
        fixture.LocalRepositoryFixture.Fetch(origin);

        var localRepository = fixture.LocalRepositoryFixture.Repository.ToGitRepository();

        var strategy = GetVersionStrategy(localRepository);
        var branch = localRepository.FindBranch(branchName);

        var configuration = GitFlowConfigurationBuilder.New.Build();
        var effectiveBranchConfiguration = configuration.GetEffectiveBranchConfiguration(branch);

        strategy.ShouldNotBeNull();
        var baseVersion = strategy.GetBaseVersions(effectiveBranchConfiguration).Single();

        baseVersion.GetSemanticVersion().ToString().ShouldBe(expectedBaseVersion);
    }

    private static IVersionStrategy GetVersionStrategy(IGitRepository repository,
        string? targetBranch = null, IReadOnlyDictionary<object, object?>? overrideConfiguration = null)
    {
        var serviceProvider = BuildServiceProvider(repository, targetBranch, overrideConfiguration);
        return serviceProvider.GetServiceForType<IVersionStrategy, VersionInBranchNameVersionStrategy>();
    }
}
