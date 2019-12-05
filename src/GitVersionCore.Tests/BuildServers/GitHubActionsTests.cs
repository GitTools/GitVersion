using System;
using GitVersion;
using GitVersion.BuildServers;
using GitVersion.Logging;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using GitVersion.Configuration;
using NSubstitute;
using Environment = System.Environment;

namespace GitVersionCore.Tests.BuildServers
{

    [TestFixture]
    public class GitHubActionsTests : TestBase
    {
        private IConsole console;
        private IEnvironment environment;
        private ILog log;
        private List<string> consoleLinesWritten;

        [SetUp]
        public void SetUp()
        {
            log = new NullLog();
            console = Substitute.For<IConsole>();

            consoleLinesWritten = new List<string>();
            console.WhenForAnyArgs(c => c.WriteLine(default))
                .Do(info => consoleLinesWritten.Add(info.Arg<string>()));
            console.WhenForAnyArgs(c => c.WriteLine())
                .Do(info => consoleLinesWritten.Add(string.Empty));

            environment = new TestEnvironment();
            environment.SetEnvironmentVariable("GITHUB_ACTION", Guid.NewGuid().ToString());
        }

        [TearDown]
        public void TearDown()
        {
            environment.SetEnvironmentVariable("GITHUB_ACTION", null);
            environment.SetEnvironmentVariable("GitVersion_Major", null);
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeTrueWhenEnvironmentVariableIsSet()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log, console);

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeFalseWhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            environment.SetEnvironmentVariable("GITHUB_ACTION", "");
            var buildServer = new GitHubActions(environment, log, console);

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public void GetCurrentBranchShouldGetBranchIfSet()
        {
            // Arrange
            const string expected = "actionsBranch";

            environment.SetEnvironmentVariable("GITHUB_REF", $"refs/heads/{expected}");

            var buildServer = new GitHubActions(environment, log, console);

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe(expected);
        }

        [Test]
        public void GetCurrentBranchShouldNotMatchTag()
        {
            // Arrange
            environment.SetEnvironmentVariable("GITHUB_REF", $"refs/tags/v1.0.0");

            var buildServer = new GitHubActions(environment, log, console);

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBeNull();
        }

        [TestCase("Something", "1.0.0",
            "Adding Environment Variable to current and future steps. name='GitVersion_Something' value='1.0.0'",
            "::set-env name=GitVersion_Something::1.0.0")]
        public void GetSetParameterMessage(string key, string value, string expectedResult, string expectedConsole)
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log, console);

            // Assert
            environment.GetEnvironmentVariable("GitVersion_Something").ShouldBeNullOrWhiteSpace();

            // Act
            var result = buildServer.GenerateSetParameterMessage(key, value);

            // Assert
            result.ShouldContain(s => true, 1);
            result.ShouldBeEquivalentTo(new[] { expectedResult });

            consoleLinesWritten.ShouldBeEquivalentTo(new List<string> { expectedConsole });

            environment.GetEnvironmentVariable("GitVersion_Something").ShouldBe("1.0.0");
        }

        [Test]
        public void SkipEmptySetParameterMessage()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log, console);

            // Act
            var result = buildServer.GenerateSetParameterMessage("Hello", string.Empty);

            // Assert
            result.ShouldBeEquivalentTo(new string[0]);
        }

        [Test]
        public void ShouldWriteIntegration()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log, console);

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
                "Adding Environment Variable to current and future steps. name='GitVersion_Major' value='1.0.0'"
            };

            string.Join(Environment.NewLine, list)
                .ShouldBe(string.Join(Environment.NewLine, expected));

            consoleLinesWritten.ShouldBeEquivalentTo(new List<string> { "::set-env name=GitVersion_Major::1.0.0" });

            environment.GetEnvironmentVariable("GitVersion_Major").ShouldBe("1.0.0");
        }

        [Test]
        public void GetEmptyGenerateSetVersionMessage()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log, console);
            var vars = new TestableVersionVariables("1.0.0");

            // Act
            var message = buildServer.GenerateSetVersionMessage(vars);

            // Assert
            message.ShouldBeEmpty();
        }
    }
}
