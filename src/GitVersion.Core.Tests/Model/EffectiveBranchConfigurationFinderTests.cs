using GitVersion.Common;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
public class EffectiveBranchConfigurationFinderTests
{
    [Test]
    public void When_getting_configurations_with_an_orphaned_branch_Given_configuaration_with_increment_null_Then_result_should_be_empty()
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
    public void When_getting_configurations_with_an_orphaned_branch_Given_configuration_with_increment_inherit_Then_result_should_have_increment_none()
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
    public void When_getting_configurations_with_an_orphaned_branch_Given_configuration_with_increment_Then_result_should_have_same_increment(
        IncrementStrategy increment)
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("develop", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = ConfigBuilder.New.WithIncrement(increment).WithIncrement("develop", IncrementStrategy.Inherit).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Configuration.Increment.ShouldBe(increment);
    }

    [Test]
    public void When_getting_configurations_with_an_unknown_branch_Given_configuaration_with_increment_null_and_fallback_with_increment_inherit_Then_result_should_be_empty()
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
    //public void When_getting_configurations_with_an_unknown_branch_Given_configuaration_with_increment_and_fallback_with_increment_none_Then_result_should_have_always_increment_none(
    //    IncrementStrategy increment)
    //{
    //    // Arrange
    //    var branchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
    //    var configuration = ConfigBuilder.New.WithIncrement(increment).Build();
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
    public void When_getting_configurations_with_an_unknown_branch_Given_configuaration_with_increment_and_fallback_with_increment_inherit_Then_result_should_have_same_increment(
        IncrementStrategy increment)
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("unknown", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = ConfigBuilder.New.WithIncrement(increment).Build();
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        repositoryStoreMock.GetTargetBranches(branchMock, configuration, Arg.Any<IBranch[]>()).Returns(Enumerable.Empty<IBranch>());

        var unitUnderTest = new EffectiveBranchConfigurationFinder(Substitute.For<ILog>(), repositoryStoreMock);

        // Act
        var actual = unitUnderTest.GetConfigurations(branchMock, configuration).ToArray();

        // Assert
        actual.ShouldHaveSingleItem();
        actual[0].Branch.ShouldBe(branchMock);
        actual[0].Configuration.Increment.ShouldBe(increment);
    }
}
