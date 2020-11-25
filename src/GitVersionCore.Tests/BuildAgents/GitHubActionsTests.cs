using System.Collections.Generic;
using System.IO;
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
        private string githubSetEnvironmentTempFilePath;

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

            githubSetEnvironmentTempFilePath = Path.GetTempFileName();
            environment.SetEnvironmentVariable(GitHubActions.GitHubSetEnvTempFileEnvironmentVariableName, githubSetEnvironmentTempFilePath);
        }

        [TearDown]
        public void TearDown()
        {
            environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, null);
            environment.SetEnvironmentVariable(GitHubActions.GitHubSetEnvTempFileEnvironmentVariableName, null);
            if (githubSetEnvironmentTempFilePath != null && File.Exists(githubSetEnvironmentTempFilePath))
            {
                File.Delete(githubSetEnvironmentTempFilePath);
                githubSetEnvironmentTempFilePath = null;
            }
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

        [Test]
        public void GetSetParameterMessage()
        {
            // Assert
            environment.GetEnvironmentVariable("GitVersion_Something").ShouldBeNullOrWhiteSpace();

            // Act
            var result = buildServer.GenerateSetParameterMessage("GitVersion_Something", "1.0.0");

            // Assert
            result.ShouldContain(s => true, 0);
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
                "Writing version variables to $GITHUB_ENV file for 'GitHubActions'."
            };

            string.Join(Environment.NewLine, list)
                .ShouldBe(string.Join(Environment.NewLine, expected));

            var expectedFileContents = new List<string>
            {
                "GitVersion_Major=1.0.0"
            };

            var actualFileContents = File.ReadAllLines(githubSetEnvironmentTempFilePath);

            actualFileContents.ShouldBe(expectedFileContents);
        }

        [Test]
        public void ShouldNotWriteIntegration()
        {
            // Arrange
            var vars = new TestableVersionVariables("1.0.0");

            var list = new List<string>();

            // Assert
            environment.GetEnvironmentVariable("GitVersion_Major").ShouldBeNullOrWhiteSpace();

            // Act
            buildServer.WriteIntegration(s => { list.Add(s); }, vars, false);

            list.ShouldNotContain(x => x.StartsWith("Executing GenerateSetVersionMessage for "));
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
