using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class EffectiveBranchConfigurationFinderTests
{
    [Theory]
    public void When_getting_configurations_of_a_branch_without_versioning_mode_Given_fallback_configuration_with_versioning_mode_Then_result_should_have_versioning_mode(
        DeploymentMode versioningMode)
    {
        // Arrange
        var branchMock = GitRepositoryTestingExtensions.CreateMockBranch("main", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithDeploymentMode(versioningMode)
            .WithBranch("main", builder => builder.WithDeploymentMode(null))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Value.DeploymentMode.ShouldBe(versioningMode);
    }

    [Theory]
    public void When_getting_configurations_of_a_branch_with_versioning_mode_Given_fallback_configuration_without_versioning_mode_Then_result_should_have_versioning_mode(
        DeploymentMode versioningMode)
    {
        // Arrange
        var mainBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("main", GitRepositoryTestingExtensions.CreateMockCommit());
        var developBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithDeploymentMode(null)
            .WithBranch("main", builder => builder.WithDeploymentMode(versioningMode))
            .WithBranch("develop", builder => builder.WithDeploymentMode(null).WithIncrement(IncrementStrategy.Inherit))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([mainBranchMock]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        actual[0].Value.DeploymentMode.ShouldBe(versioningMode);
    }

    [Theory]
    public void When_getting_configurations_of_a_branch_with_versioning_mode_Given_parent_configuration_with_versioning_mode_Then_result_should_not_have_versioning_mode_of_parent(
        DeploymentMode versioningMode)
    {
        // Arrange
        var mainBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("main", GitRepositoryTestingExtensions.CreateMockCommit());
        var developBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithDeploymentMode(null)
            .WithBranch("main", builder => builder.WithDeploymentMode(versioningMode))
            .WithBranch("develop", builder => builder
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery).WithIncrement(IncrementStrategy.Inherit)
            )
            .Build();

        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([mainBranchMock]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        if (versioningMode == DeploymentMode.ContinuousDelivery)
        {
            actual[0].Value.DeploymentMode.ShouldBe(versioningMode);
        }
        else
        {
            actual[0].Value.DeploymentMode.ShouldNotBe(versioningMode);
        }
    }

    [Test]
    public void When_getting_configurations_of_a_branch_with_tag_alpha_Given_branch_which_inherits_from_parent_branch_Then_result_should_have_tag_alpha()
    {
        // Arrange
        var mainBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("main", GitRepositoryTestingExtensions.CreateMockCommit());
        var developBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithLabel(string.Empty))
            .WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Inherit).WithLabel("alpha")
            )
            .Build();

        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([mainBranchMock]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        actual[0].Value.Label.ShouldBe("alpha");
    }

    [Test]
    public void When_getting_configurations_of_a_branch_without_tag_Given_branch_which_inherits_from_parent_branch_Then_result_should_have_tag_from_parent()
    {
        // Arrange
        var mainBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("main", GitRepositoryTestingExtensions.CreateMockCommit());
        var developBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithLabel(string.Empty))
            .WithBranch("develop", builder => builder
                .WithIncrement(IncrementStrategy.Inherit).WithLabel(null)
            )
            .Build();

        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([mainBranchMock]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        actual[0].Value.Label.ShouldBe(string.Empty);
    }

    [TestCase("release/latest", IncrementStrategy.None, "latest")]
    [TestCase("release/1.0.0", IncrementStrategy.Patch, "not-latest")]
    public void UsesFirstBranchConfigWhenMultipleMatch(string branchName, IncrementStrategy incrementStrategy, string label)
    {
        // Arrange
        var releaseBranchMock = GitRepositoryTestingExtensions.CreateMockBranch(branchName, GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch("release/latest", builder => builder
                .WithIncrement(IncrementStrategy.None)
                .WithLabel("latest")
                .WithRegularExpression("release/latest")
            )
            .WithBranch("release", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithLabel("not-latest")
                .WithRegularExpression(@"releases?[\/-]")
            )
            .Build();

        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(releaseBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(releaseBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(releaseBranchMock);
        actual[0].Value.Increment.ShouldBe(incrementStrategy);
        actual[0].Value.Label.ShouldBe(label);
    }

    [Test]
    public void When_getting_configurations_of_an_orphaned_branch_Given_fallback_configuration_with_increment_inherit_Then_result_should_be_empty()
    {
        // Arrange
        var branchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithIncrement(IncrementStrategy.Inherit)
            .WithBranch("develop", builder => builder.WithIncrement(IncrementStrategy.Inherit))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldBeEmpty();
    }

    [TestCase(IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_orphaned_branch_Given_fallback_configuration_with_increment_Then_result_should_have_fallback_increment(
        IncrementStrategy fallbackIncrement)
    {
        // Arrange
        var branchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithIncrement(fallbackIncrement)
            .WithBranch("develop", builder => builder.WithIncrement(IncrementStrategy.Inherit))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Value.Increment.ShouldBe(fallbackIncrement);
    }

    [Test]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_and_unknown_configuration_with_increment_inherit_Then_result_should_be_empty()
    {
        // Arrange
        var branchMock = GitRepositoryTestingExtensions.CreateMockBranch("unknown", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithIncrement(IncrementStrategy.Inherit)
            .WithBranch("unknown", builder => builder.WithIncrement(IncrementStrategy.Inherit))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldBeEmpty();
    }

    [TestCase(IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuration_with_increment_and_unknown_configuration_with_increment_inherit_Then_result_should_have_fallback_increment(
    IncrementStrategy fallbackIncrement)
    {
        // Arrange
        var branchMock = GitRepositoryTestingExtensions.CreateMockBranch("unknown", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithIncrement(fallbackIncrement)
            .WithBranch("unknown", builder => builder.WithIncrement(IncrementStrategy.Inherit))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Value.Increment.ShouldBe(fallbackIncrement);
    }

    [Theory]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuration_with_increment_and_develop_branch_with_increment_Then_result_should_have_develop_increment(
        IncrementStrategy fallbackIncrement, IncrementStrategy developIncrement)
    {
        // Arrange
        var unknownBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("unknown", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithIncrement(fallbackIncrement)
            .WithBranch("develop", builder => builder.WithIncrement(developIncrement))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetSourceBranches(unknownBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([developBranchMock]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        if (fallbackIncrement == IncrementStrategy.Inherit && developIncrement == IncrementStrategy.Inherit)
        {
            actual.ShouldBeEmpty();
        }
        else
        {
            actual.ShouldHaveSingleItem();
            actual[0].Branch.ShouldBe(developBranchMock);

            actual[0].Value.Increment.ShouldBe(developIncrement == IncrementStrategy.Inherit ? fallbackIncrement : developIncrement);
        }
    }

    [Theory]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuration_with_increment_and_develop_branch_with_increment_inherit_Then_result_should_have_fallback_increment(
        IncrementStrategy fallbackIncrement)
    {
        // Arrange
        var unknownBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("unknown", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithIncrement(fallbackIncrement)
            .WithBranch("develop", builder => builder.WithIncrement(IncrementStrategy.Inherit))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetSourceBranches(unknownBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns([developBranchMock]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        if (fallbackIncrement == IncrementStrategy.Inherit)
        {
            actual.ShouldBeEmpty();
        }
        else
        {
            actual.ShouldHaveSingleItem();
            actual[0].Branch.ShouldBe(developBranchMock);
            actual[0].Value.Increment.ShouldBe(fallbackIncrement);
        }
    }

    [TestCase(IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_and_unknown_configuration_with_increment_inherit_and_develop_branch_with_increment_Then_result_should_have_develop_branch_increment(
        IncrementStrategy developBranchIncrement)
    {
        // Arrange
        var unknownBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("unknown", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GitFlowConfigurationBuilder.New
            .WithIncrement(IncrementStrategy.Inherit)
            .WithBranch("develop", builder => builder.WithIncrement(developBranchIncrement))
            .Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitRepositoryTestingExtensions.CreateMockBranch("develop", GitRepositoryTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetSourceBranches(Arg.Any<IBranch>(), Arg.Any<GitVersionConfiguration>(), Arg.Any<HashSet<IBranch>>()).Returns([developBranchMock]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(developBranchMock);
        actual[0].Value.Increment.ShouldBe(developBranchIncrement);
    }

    [Test]
    public void Pull_request_merge_target_selects_local_support_branch_from_multiple_candidates()
    {
        // Arrange
        var pullRequestBranch = CreatePullRequestBranch("Merge pull request 2 from hotfix/v1.0.2 into support/v1.0.x");
        var mainBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/main", GitRepositoryTestingExtensions.CreateMockCommit());
        var localSupportBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/support/v1.0.x", GitRepositoryTestingExtensions.CreateMockCommit());
        var remoteSupportBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/remotes/origin/support/v1.0.x", GitRepositoryTestingExtensions.CreateMockCommit());
        remoteSupportBranch.IsRemote.Returns(true);
        var configuration = GetPullRequestConfiguration();
        var repositoryStore = Substitute.For<IRepositoryStore>();
        SetBranches(repositoryStore, mainBranch, remoteSupportBranch, localSupportBranch);
        repositoryStore.GetSourceBranches(pullRequestBranch, configuration, Arg.Any<HashSet<IBranch>>())
            .Returns([mainBranch]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStore);

        // Act
        var actual = unitUnderTest.GetConfigurations(pullRequestBranch, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem().Branch.ShouldBe(localSupportBranch);
        actual[0].Value.Increment.ShouldBe(IncrementStrategy.Patch);
    }

    [Test]
    public void Pull_request_merge_target_accepts_remote_only_branch()
    {
        // Arrange
        var pullRequestBranch = CreatePullRequestBranch("Merge pull request 2 from hotfix/v1.0.2 into support/v1.0.x");
        var mainBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/main", GitRepositoryTestingExtensions.CreateMockCommit());
        var remoteSupportBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/remotes/origin/support/v1.0.x", GitRepositoryTestingExtensions.CreateMockCommit());
        remoteSupportBranch.IsRemote.Returns(true);
        var configuration = GetPullRequestConfiguration();
        var repositoryStore = Substitute.For<IRepositoryStore>();
        SetBranches(repositoryStore, mainBranch, remoteSupportBranch);
        repositoryStore.GetSourceBranches(pullRequestBranch, configuration, Arg.Any<HashSet<IBranch>>())
            .Returns([mainBranch]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStore);

        // Act
        var actual = unitUnderTest.GetConfigurations(pullRequestBranch, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem().Branch.ShouldBe(remoteSupportBranch);
    }

    [TestCase("Merge pull request 2 from hotfix/v1.0.2", "refs/pull/2/merge")]
    [TestCase("Merge pull request 2 from hotfix/v1.0.2 into release/v1.0.x", "refs/pull/2/merge")]
    [TestCase("Merge pull request 2 from hotfix/v1.0.2 into support/v1.0.x", "refs/heads/feature/foo")]
    public void Pull_request_target_falls_back_to_all_source_candidates_when_it_cannot_disambiguate(string message, string branchName)
    {
        // Arrange
        var branch = CreateMergeBranch(branchName, message);
        var mainBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/main", GitRepositoryTestingExtensions.CreateMockCommit());
        var supportBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/support/v1.0.x", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GetPullRequestConfiguration();
        var repositoryStore = Substitute.For<IRepositoryStore>();
        SetBranches(repositoryStore, mainBranch, supportBranch);
        repositoryStore.GetSourceBranches(branch, configuration, Arg.Any<HashSet<IBranch>>()).Returns([mainBranch, supportBranch]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStore);

        // Act
        var actual = unitUnderTest.GetConfigurations(branch, configuration).ToArray();

        // Assert
        actual.Select(x => x.Branch).ShouldBe([mainBranch, supportBranch]);
    }

    [Test]
    public void Pull_request_target_continues_inheriting_from_selected_branch()
    {
        // Arrange
        var pullRequestBranch = CreatePullRequestBranch("Merge pull request 2 from hotfix/v1.0.2 into support/v1.0.x");
        var supportBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/support/v1.0.x", GitRepositoryTestingExtensions.CreateMockCommit());
        var mainBranch = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/main", GitRepositoryTestingExtensions.CreateMockCommit());
        var configuration = GetPullRequestConfiguration(supportIncrement: IncrementStrategy.Inherit);
        var repositoryStore = Substitute.For<IRepositoryStore>();
        SetBranches(repositoryStore, mainBranch, supportBranch);
        repositoryStore.GetSourceBranches(pullRequestBranch, configuration, Arg.Any<HashSet<IBranch>>()).Returns([mainBranch]);
        repositoryStore.GetSourceBranches(supportBranch, configuration, Arg.Any<HashSet<IBranch>>()).Returns([mainBranch]);

        var unitUnderTest = new EffectiveBranchConfigurationFinder(NullLogger<EffectiveBranchConfigurationFinder>.Instance, repositoryStore);

        // Act
        var actual = unitUnderTest.GetConfigurations(pullRequestBranch, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem().Branch.ShouldBe(mainBranch);
        actual[0].Value.Increment.ShouldBe(IncrementStrategy.Minor);
    }

    private static IBranch CreatePullRequestBranch(string message) => CreateMergeBranch("refs/pull/2/merge", message);

    private static void SetBranches(IRepositoryStore repositoryStore, params IBranch[] branches)
    {
        var branchCollection = Substitute.For<IBranchCollection>();
        branchCollection.MockCollectionReturn(branches);
        repositoryStore.Branches.Returns(branchCollection);
    }

    private static IBranch CreateMergeBranch(string branchName, string message)
    {
        var mergeCommit = GitRepositoryTestingExtensions.CreateMockCommit();
        mergeCommit.Message.Returns(message);
        mergeCommit.IsMergeCommit.Returns(true);
        return GitRepositoryTestingExtensions.CreateMockBranch(branchName, mergeCommit);
    }

    private static IGitVersionConfiguration GetPullRequestConfiguration(IncrementStrategy supportIncrement = IncrementStrategy.Patch) =>
        GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithIncrement(IncrementStrategy.Minor))
            .WithBranch("pull-request", builder => builder.WithIncrement(IncrementStrategy.Inherit))
            .WithBranch("support", builder => builder.WithIncrement(supportIncrement))
            .Build();
}
