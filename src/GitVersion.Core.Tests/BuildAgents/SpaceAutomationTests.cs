using GitVersion;
using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.BuildAgents;

[TestFixture]
public class SpaceAutomationTests : TestBase
{
    private IEnvironment environment;
    private SpaceAutomation buildServer;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<SpaceAutomation>());
        this.environment = sp.GetService<IEnvironment>();
        this.buildServer = sp.GetService<SpaceAutomation>();
        this.environment.SetEnvironmentVariable(SpaceAutomation.EnvironmentVariableName, "true");
    }

    [TearDown]
    public void TearDown() => this.environment.SetEnvironmentVariable(SpaceAutomation.EnvironmentVariableName, null);

    [Test]
    public void CanApplyToCurrentContextShouldBeTrueWhenEnvironmentVariableIsSet()
    {
        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void CanApplyToCurrentContextShouldBeFalseWhenEnvironmentVariableIsNotSet()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(SpaceAutomation.EnvironmentVariableName, "");

        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void GetCurrentBranchShouldHandleBranches()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("JB_SPACE_GIT_BRANCH", "refs/heads/master");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/heads/master");
    }

    [Test]
    public void GetCurrentBranchShouldHandleTags()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("JB_SPACE_GIT_BRANCH", "refs/tags/1.0.0");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/tags/1.0.0");
    }

    [Test]
    public void GetCurrentBranchShouldHandlePullRequests()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("JB_SPACE_GIT_BRANCH", "refs/pull/1/merge");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/pull/1/merge");
    }

    [Test]
    public void GetEmptyGenerateSetVersionMessage()
    {
        // Arrange
        var vars = new TestableVersionVariables("1.0.0");

        // Act
        var message = this.buildServer.GenerateSetVersionMessage(vars);

        // Assert
        message.ShouldBeEmpty();
    }
}
