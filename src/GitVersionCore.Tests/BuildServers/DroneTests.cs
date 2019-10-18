using GitVersion.BuildServers;
using GitVersion.Common;
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
        public void CanApplyToCurrentContext_ShouldBeTrue_WhenEnvironmentVariableIsSet()
        {
            // Arrange
            var buildServer = new Drone(environment, log);

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContext_ShouldBeFalse_WhenEnvironmentVariableIsNotSet()
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
        public void GetCurrentBranch_ShouldDroneBranch_InCaseOfPush()
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
        public void GetCurrentBranch_ShouldUseDroneSourceBranch_InCaseOfPullRequestAndNonEmptyDroneSourceBranch()
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
        public void GetCurrentBranch_ShouldUseSourceBranchFromCiCommitRefSpec_InCaseOfPullRequestAndEmptyDroneSourceBranch()
        {
            // Arrange
            const string droneSourceBranch = "droneSourceBranch";
            const string droneDestinationBranch = "droneDestinationBranch";

            string ciCommitRefSpec = $"{droneSourceBranch}:{droneDestinationBranch}";

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
        public void GetCurrentBranch_ShouldUseDroneBranch_InCaseOfPullRequestAndEmptyDroneSourceBranchAndCiCommitRefSpec()
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
        public void GetCurrentBranch_ShouldUseDroneBranch_InCaseOfPullRequestAndEmptyDroneSourceBranchAndInvalidFormatOfCiCommitRefSpec()
        {
            // Arrange
            const string droneBranch = "droneBranch";
            const string droneSourceBranch = "droneSourceBranch";
            const string droneDestinationBranch = "droneDestinationBranch";

            string ciCommitRefSpec = $"{droneSourceBranch};{droneDestinationBranch}";

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
