namespace GitVersionCore.Tests.BuildServers
{
    using System;
    using GitVersion;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class DroneTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("DRONE", "true");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("DRONE", null);
        }

        [Test]
        public void CanApplyToCurrentContext_ShouldBeTrue_WhenEnvironmentVariableIsSet()
        {
            // Arrange
            var buildServer = new Drone();

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContext_ShouldBeFalse_WhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("DRONE", "");
            var buildServer = new Drone();

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

            Environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "");
            Environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

            var buildServer = new Drone();

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
            Environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            Environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", droneSourceBranch);

            var buildServer = new Drone();

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

            Environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            Environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
            Environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", ciCommitRefSpec);

            var buildServer = new Drone();

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

            Environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            Environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
            Environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", "");
            Environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

            var buildServer = new Drone();

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

            Environment.SetEnvironmentVariable("DRONE_PULL_REQUEST", "1");
            Environment.SetEnvironmentVariable("DRONE_SOURCE_BRANCH", "");
            Environment.SetEnvironmentVariable("CI_COMMIT_REFSPEC", ciCommitRefSpec);
            Environment.SetEnvironmentVariable("DRONE_BRANCH", droneBranch);

            var buildServer = new Drone();

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe(droneBranch);
        }
    }
}
