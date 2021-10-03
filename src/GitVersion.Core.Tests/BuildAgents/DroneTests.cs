using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class DroneTests : TestBase
{
    private IEnvironment environment;
    private IServiceProvider sp;
    private Drone buildServer;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<Drone>());
        this.environment = this.sp.GetService<IEnvironment>();
        this.buildServer = this.sp.GetService<Drone>();
        this.environment.SetEnvironmentVariable("DRONE", "true");
    }

    [TearDown]
    public void TearDown() => this.environment.SetEnvironmentVariable("DRONE", null);

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
        this.environment.SetEnvironmentVariable("DRONE", "");

        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void GetCurrentBranchShouldDroneBranchInCaseOfPush()
    {
        // Arrange
        const string droneBranch = "droneBranch";

        this.environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "");
        this.environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe(droneBranch);
    }

    [Test]
    public void GetCurrentBranchShouldUseDroneSourceBranchInCaseOfPullRequestAndNonEmptyDroneSourceBranch()
    {
        // Arrange
        const string droneSourceBranch = "droneSourceBranch";
        this.environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
        this.environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", droneSourceBranch);

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe(droneSourceBranch);
    }

    [Test]
    public void GetCurrentBranchShouldUseSourceBranchFromCiCommitRefSpecInCaseOfPullRequestAndEmptyDroneSourceBranch()
    {
        // Arrange
        const string droneSourceBranch = "droneSourceBranch";
        const string droneDestinationBranch = "droneDestinationBranch";

        var ciCommitRefSpec = $"{droneSourceBranch}:{droneDestinationBranch}";

        this.environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
        this.environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
        this.environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", ciCommitRefSpec);

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe(droneSourceBranch);
    }

    [Test]
    public void GetCurrentBranchShouldUseDroneBranchInCaseOfPullRequestAndEmptyDroneSourceBranchAndCiCommitRefSpec()
    {
        // Arrange
        const string droneBranch = "droneBranch";

        this.environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
        this.environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
        this.environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", "");
        this.environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe(droneBranch);
    }

    [Test]
    public void GetCurrentBranchShouldUseDroneBranchInCaseOfPullRequestAndEmptyDroneSourceBranchAndInvalidFormatOfCiCommitRefSpec()
    {
        // Arrange
        const string droneBranch = "droneBranch";
        const string droneSourceBranch = "droneSourceBranch";
        const string droneDestinationBranch = "droneDestinationBranch";

        var ciCommitRefSpec = $"{droneSourceBranch};{droneDestinationBranch}";

        this.environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
        this.environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
        this.environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", ciCommitRefSpec);
        this.environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe(droneBranch);
    }
}
