using System.Collections.Generic;
using GitVersion;
using GitVersion.BuildAgents;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Environment = System.Environment;

namespace GitVersionCore.Tests.BuildAgents
{

    [TestFixture]
    public class GitHubActionsTests : TestBase
    {
        private IEnvironment environment;
        private GitHubActions buildServer;

        [SetUp]
        public void SetUp()
        {
            var sp = ConfigureServices(services =>
            {
                services.AddSingleton<GitHubActions>();
            });
            environment = sp.GetService<IEnvironment>();
            buildServer = sp.GetService<GitHubActions>();
            environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, "true");
        }

        [TearDown]
        public void TearDown()
        {
            environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, null);
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeTrueWhenEnvironmentVariableIsSet()
        {
            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeFalseWhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, "");

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public void GetCurrentBranchShouldHandleBranches()
        {
            // Arrange
            environment.SetEnvironmentVariable("GITHUB_REF", "refs/heads/master");

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe("refs/heads/master");
        }

        [Test]
        public void GetCurrentBranchShouldHandleTags()
        {
            // Arrange
            environment.SetEnvironmentVariable("GITHUB_REF", "refs/tags/1.0.0");

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe("refs/tags/1.0.0");
        }

        [Test]
        public void GetCurrentBranchShouldHandlePullRequests()
        {
            // Arrange
            environment.SetEnvironmentVariable("GITHUB_REF", "refs/pull/1/merge");

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe("refs/pull/1/merge");
        }

        [TestCase("Something", "1.0.0",
            "::set-env name=GitVersion_Something::1.0.0")]
        public void GetSetParameterMessage(string key, string value, string expectedResult)
        {
            // Assert
            environment.GetEnvironmentVariable("GitVersion_Something").ShouldBeNullOrWhiteSpace();

            // Act
            var result = buildServer.GenerateSetParameterMessage(key, value);

            // Assert
            result.ShouldContain(s => true, 1);
            result.ShouldBeEquivalentTo(new[] { expectedResult });
        }

        [Test]
        public void SkipEmptySetParameterMessage()
        {
            // Act
            var result = buildServer.GenerateSetParameterMessage("Hello", string.Empty);

            // Assert
            result.ShouldBeEquivalentTo(new string[0]);
        }

        [Test]
        public void ShouldWriteIntegration()
        {
            // Arrange
            var vars = new TestableVersionVariables("1.0.0");

            var list = new List<string>();

            // Assert
            environment.GetEnvironmentVariable("GitVersion_Major").ShouldBeNullOrWhiteSpace();

            // Act
            buildServer.WriteIntegration(s => { list.Add(s); }, vars);

            // Assert
            var expected = new List<string>
            {
                "Executing GenerateSetVersionMessage for 'GitHubActions'.",
                "",
                "Executing GenerateBuildLogOutput for 'GitHubActions'.",
                "::set-env name=GitVersion_Major::1.0.0"
            };

            string.Join(Environment.NewLine, list)
                .ShouldBe(string.Join(Environment.NewLine, expected));
        }

        [Test]
        public void GetEmptyGenerateSetVersionMessage()
        {
            // Arrange
            var vars = new TestableVersionVariables("1.0.0");

            // Act
            var message = buildServer.GenerateSetVersionMessage(vars);

            // Assert
            message.ShouldBeEmpty();
        }
    }
}
