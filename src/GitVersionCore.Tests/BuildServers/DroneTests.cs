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
    }
}
