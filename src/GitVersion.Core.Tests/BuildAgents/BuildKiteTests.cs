using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class BuildKiteTests : TestBase
{
    private IEnvironment environment;
    private BuildKite buildServer;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<BuildKite>());
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.buildServer = sp.GetRequiredService<BuildKite>();
        this.environment.SetEnvironmentVariable(BuildKite.EnvironmentVariableName, "true");
    }

    [TearDown]
    public void TearDown() => this.environment.SetEnvironmentVariable(BuildKite.EnvironmentVariableName, null);

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
        this.environment.SetEnvironmentVariable(BuildKite.EnvironmentVariableName, "");

        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void GetCurrentBranchShouldHandleBranches()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BUILDKITE_BRANCH", MainBranch);
        this.environment.SetEnvironmentVariable("BUILDKITE_PULL_REQUEST", "false");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe(MainBranch);
    }

    [Test]
    public void GetCurrentBranchShouldHandlePullRequests()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BUILDKITE_BRANCH", "feature/new");
        this.environment.SetEnvironmentVariable("BUILDKITE_PULL_REQUEST", "55");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/pull/55/head");
    }

    [Test]
    public void GetSetParameterMessageShouldReturnEmptyArray()
    {
        // Act
        var result = this.buildServer.GenerateSetParameterMessage("Foo", "Bar");

        // Assert
        result.ShouldBeEmpty();
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
