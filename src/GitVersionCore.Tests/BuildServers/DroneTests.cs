using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class DroneTests : TestBase
    {
        private IEnvironment environment;
        private ILog log;

        [SetUp]
        public void SetUp()
        {
            log = new NullLog();
            environment = new TestEnvironment();
            environment.SetEnvironmentVariable("DRONE", "true");
        }

        [TearDown]
        public void TearDown()
        {
            environment.SetEnvironmentVariable("DRONE", null);
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeTrueWhenEnvironmentVariableIsSet()
        {
            // Arrange
            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeFalseWhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            environment.SetEnvironmentVariable("DRONE", "");
            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public void GetCurrentBranchShouldDroneBranchInCaseOfPush()
        {
            // Arrange
            const string droneBranch = "droneBranch";

            environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "");
            environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe(droneBranch);
        }

        [Test]
        public void GetCurrentBranchShouldUseDroneSourceBranchInCaseOfPullRequestAndNonEmptyDroneSourceBranch()
        {
            // Arrange
            const string droneSourceBranch = "droneSourceBranch";
            environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", droneSourceBranch);

            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.GetCurrentBranch(false);

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

            environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
            environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", ciCommitRefSpec);

            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe(droneSourceBranch);
        }

        [Test]
        public void GetCurrentBranchShouldUseDroneBranchInCaseOfPullRequestAndEmptyDroneSourceBranchAndCiCommitRefSpec()
        {
            // Arrange
            const string droneBranch = "droneBranch";

            environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
            environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", "");
            environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.GetCurrentBranch(false);

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

            environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
            environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", ciCommitRefSpec);
            environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe(droneBranch);
        }
    }
}
