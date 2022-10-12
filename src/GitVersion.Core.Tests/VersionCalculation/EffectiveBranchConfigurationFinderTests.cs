using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.VersionCalculation;

[TestFixture]
public class EffectiveBranchConfigurationFinderTests
{
    [Theory]
    public void When_getting_configurations_of_a_branch_without_versioning_mode_Given_fallback_configuaration_with_versioning_mode_Then_result_should_have_versioning_mode(
        VersioningMode versioningMode)
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithVersioningMode(versioningMode).WithoutVersioningMode("main").Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Value.VersioningMode.ShouldBe(versioningMode);
    }

    [Theory]
    public void When_getting_configurations_of_a_branch_with_versioning_mode_Given_fallback_configuaration_without_versioning_mode_Then_result_should_have_versioning_mode(
        VersioningMode versioningMode)
    {
        // Arrange
        var mainBranchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithoutVersioningMode().WithVersioningMode("main", versioningMode)
            .WithoutVersioningMode("develop").WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(new[] { mainBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        actual[0].Value.VersioningMode.ShouldBe(versioningMode);
    }

    [Theory]
    public void When_getting_configurations_of_a_branch_with_versioning_mode_Given_parent_configuaration_with_versioning_mode_Then_result_should_not_have_versioning_mode_of_parent(
        VersioningMode versioningMode)
    {
        // Arrange
        var mainBranchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithoutVersioningMode().WithVersioningMode("main", versioningMode)
            .WithVersioningMode("develop", VersioningMode.ContinuousDelivery).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(new[] { mainBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        if (versioningMode == VersioningMode.ContinuousDelivery)
        {
            actual[0].Value.VersioningMode.ShouldBe(versioningMode);
        }
        else
        {
            actual[0].Value.VersioningMode.ShouldNotBe(versioningMode);
        }
    }

    [Test]
    public void When_getting_configurations_of_a_branch_with_tag_alpha_Given_branch_which_inherits_from_parent_branch_Then_result_should_have_tag_alpha()
    {
        // Arrange
        var mainBranchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement("develop", IncrementStrategy.Inherit)
            .WithTag("main", string.Empty).WithTag("develop", "alpha").Build();

        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(new[] { mainBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();


        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        actual[0].Value.Tag.ShouldBe("alpha");
    }

    [Test]
    public void When_getting_configurations_of_a_branch_without_tag_Given_branch_which_inherits_from_parent_branch_Then_result_should_have_tag_from_parent()
    {
        // Arrange
        var mainBranchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement("develop", IncrementStrategy.Inherit)
            .WithTag("main", string.Empty).WithoutTag("develop").Build();

        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(developBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(new[] { mainBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(developBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(mainBranchMock);
        actual[0].Value.Tag.ShouldBe(string.Empty);
    }

    [TestCase("release/latest", IncrementStrategy.None, "latest")]
    [TestCase("release/1.0.0", IncrementStrategy.Patch, "not-latest")]
    public void UsesFirstBranchConfigWhenMultipleMatch(string branchName, IncrementStrategy incrementStrategy, string tag)
    {
        // Arrange
        var releaseBranchMock = GitToolsTestingExtensions.CreateMockBranch(branchName, GitToolsTestingExtensions.CreateMockCommit());
        var branchConfiguration = new BranchConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            Increment = IncrementStrategy.None,
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            SourceBranches = new HashSet<string>()
        };
        var configuration = new ConfigurationBuilder().Add(new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Branches =
            {
                { "release/latest", new BranchConfiguration(branchConfiguration) { Increment = IncrementStrategy.None, Tag = "latest", Regex = "release/latest" } },
                { "release", new BranchConfiguration(branchConfiguration) { Increment = IncrementStrategy.Patch, Tag = "not-latest", Regex = "releases?[/-]" } }
            }
        }).Build();

        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(releaseBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(releaseBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(releaseBranchMock);
        actual[0].Value.Increment.ShouldBe(incrementStrategy);
        actual[0].Value.Tag.ShouldBe(tag);
    }

    [Test]
    public void When_getting_configurations_of_an_orphaned_branch_Given_fallback_configuaration_without_increment_Then_result_should_be_empty()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(null).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldBeEmpty();
    }

    [Test]
    public void When_getting_configurations_of_an_orphaned_branch_Given_fallback_configuration_with_increment_inherit_Then_result_should_have_increment_none()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(IncrementStrategy.Inherit).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Value.Increment.ShouldBe(IncrementStrategy.None);
    }

    [TestCase(IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_orphaned_branch_Given_fallback_configuration_with_increment_Then_result_should_have_fallback_increment(
        IncrementStrategy fallbackIncrement)
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(fallbackIncrement).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Value.Increment.ShouldBe(fallbackIncrement);
    }

    [Test]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_without_increment_and_unknown_configuration_with_increment_inherit_Then_result_should_be_empty()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(null).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldBeEmpty();
    }

    [TestCase(IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_with_increment_and_unknown_configuration_with_increment_inherit_Then_result_should_have_fallback_increment(
    IncrementStrategy fallbackIncrement)
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(fallbackIncrement).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Value.Increment.ShouldBe(fallbackIncrement);
    }

    [Theory]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_with_increment_and_develop_branch_with_increment_Then_result_should_have_develop_increment(
        IncrementStrategy fallbackIncrement, IncrementStrategy developBranchIncrement)
    {
        // Arrange
        var unknownBranchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(fallbackIncrement).WithIncrement("develop", developBranchIncrement).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetSourceBranches(unknownBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(new[] { developBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(developBranchMock);

        if (developBranchIncrement == IncrementStrategy.Inherit)
        {
            if (fallbackIncrement == IncrementStrategy.Inherit)
            {
                fallbackIncrement = IncrementStrategy.None;
            }
            actual[0].Value.Increment.ShouldBe(fallbackIncrement);
        }
        else
        {
            actual[0].Value.Increment.ShouldBe(developBranchIncrement);
        }

    }

    [Theory]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_with_increment_and_develop_branch_with_increment_inherit_Then_result_should_have_fallback_increment(
        IncrementStrategy fallbackIncrement)
    {
        // Arrange
        var unknownBranchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(fallbackIncrement).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetSourceBranches(unknownBranchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(new[] { developBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(developBranchMock);

        if (fallbackIncrement == IncrementStrategy.Inherit)
        {
            fallbackIncrement = IncrementStrategy.None;
        }
        actual[0].Value.Increment.ShouldBe(fallbackIncrement);
    }

    [TestCase(IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_and_unknown_configuaration_with_increment_inherit_and_develop_branch_with_increment_Then_result_should_have_develop_branch_increment(
        IncrementStrategy developBranchIncrement)
    {
        // Arrange
        var unknownBranchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.WithIncrement(IncrementStrategy.Inherit).WithIncrement("develop", developBranchIncrement).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetSourceBranches(Arg.Any<IBranch>(), Arg.Any<Model.Configuration.GitVersionConfiguration>(), Arg.Any<HashSet<IBranch>>()).Returns(new[] { developBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(developBranchMock);
        actual[0].Value.Increment.ShouldBe(developBranchIncrement);
    }
}
