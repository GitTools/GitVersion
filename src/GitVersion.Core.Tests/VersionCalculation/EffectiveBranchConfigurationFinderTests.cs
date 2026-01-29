using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.VersionCalculation;

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
}
