using GitVersion.Common;
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
    [Test]
    public void When_getting_configurations_of_an_orphaned_branch_Given_fallback_configuaration_with_increment_null_Then_result_should_be_empty()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = ConfigBuilder.New.WithIncrement(null).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

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
        var configuration = ConfigBuilder.New.WithIncrement(IncrementStrategy.Inherit).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Configuration.Increment.ShouldBe(IncrementStrategy.None);
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
        var configuration = ConfigBuilder.New.WithIncrement(fallbackIncrement).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Configuration.Increment.ShouldBe(fallbackIncrement);
    }

    [Test]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_with_increment_null_and_unknown_configuration_with_increment_inherit_Then_result_should_be_empty()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = ConfigBuilder.New.WithIncrement(null).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldBeEmpty();
    }

    // until now the fallback configuration increment is always IncrementStrategy.Inherit
    //[TestCase(IncrementStrategy.None)]
    //[TestCase(IncrementStrategy.Patch)]
    //[TestCase(IncrementStrategy.Minor)]
    //[TestCase(IncrementStrategy.Major)]
    //public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_with_increment_and_unknown_configuration_with_increment_none_Then_result_should_have_increment_none(
    //    IncrementStrategy fallbackIncrement)
    //{
    //    // Arrange
    //    var branchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
    //    var configuration = ConfigBuilder.New.WithIncrement(fallbackIncrement).Build();
    //    var repositoryStoreMock = Substitute.For<IRepositoryStore>();
    //    repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

    //    var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

    //    // Act
    //    var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

    //    // Assert
    //    actual.ShouldHaveSingleItem();
    //    actual[0].Branch.ShouldBe(branchMock);
    //    actual[0].Configuration.Increment.ShouldBe(IncrementStrategy.None);
    //}

    [TestCase(IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_with_increment_and_unknown_configuration_with_increment_inherit_Then_result_should_have_fallback_increment(
        IncrementStrategy fallbackIncrement)
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = ConfigBuilder.New.WithIncrement(fallbackIncrement).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Configuration.Increment.ShouldBe(fallbackIncrement);
    }

    [TestCase(IncrementStrategy.None, IncrementStrategy.None)]
    [TestCase(IncrementStrategy.None, IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.None, IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.None, IncrementStrategy.Major)]
    [TestCase(IncrementStrategy.Patch, IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major)]
    [TestCase(IncrementStrategy.Minor, IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major)]
    [TestCase(IncrementStrategy.Major, IncrementStrategy.None)]
    [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch)]
    [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor)]
    [TestCase(IncrementStrategy.Major, IncrementStrategy.Major)]
    public void When_getting_configurations_of_an_unknown_branch_Given_fallback_configuaration_with_increment_and_develop_branch_with_increment_Then_result_should_have_fallback_increment(
        IncrementStrategy fallbackIncrement, IncrementStrategy developBranchIncrement)
    {
        // Arrange
        var unknownBranchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = ConfigBuilder.New.WithIncrement(fallbackIncrement).WithIncrement("develop", developBranchIncrement).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetTargetBranches(unknownBranchMock, configuration, Arg.Any<IBranch[]>()).Returns(new[] { developBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(unknownBranchMock);
        actual[0].Configuration.Increment.ShouldBe(fallbackIncrement);
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
        var configuration = ConfigBuilder.New.WithIncrement(IncrementStrategy.Inherit).WithIncrement("develop", developBranchIncrement).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var developBranchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        repositoryStoreMock.GetTargetBranches(Arg.Any<IBranch>(), Arg.Any<Config>(), Arg.Any<HashSet<IBranch>>()).Returns(new[] { developBranchMock });

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(unknownBranchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(developBranchMock);
        actual[0].Configuration.Increment.ShouldBe(developBranchIncrement);
    }
}
