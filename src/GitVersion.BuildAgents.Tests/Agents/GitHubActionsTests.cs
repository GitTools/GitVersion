using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents.Tests;

[TestFixture]
public class GitHubActionsTests : TestBase
{
    private IEnvironment environment;
    private GitHubActions buildServer;
    private string? githubSetEnvironmentTempFilePath;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<GitHubActions>());
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.buildServer = sp.GetRequiredService<GitHubActions>();
        this.environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, "true");

        this.githubSetEnvironmentTempFilePath = Path.GetTempFileName();
        this.environment.SetEnvironmentVariable(GitHubActions.GitHubSetEnvTempFileEnvironmentVariableName, this.githubSetEnvironmentTempFilePath);
    }

    [TearDown]
    public void TearDown()
    {
        this.environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, null);
        this.environment.SetEnvironmentVariable(GitHubActions.GitHubSetEnvTempFileEnvironmentVariableName, null);
        if (this.githubSetEnvironmentTempFilePath == null || !File.Exists(this.githubSetEnvironmentTempFilePath))
            return;

        File.Delete(this.githubSetEnvironmentTempFilePath);
        this.githubSetEnvironmentTempFilePath = null;
    }

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
        this.environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, "");

        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void GetCurrentBranchShouldHandleBranches()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("GITHUB_REF", $"refs/heads/{MainBranch}");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe($"refs/heads/{MainBranch}");
    }

    [Test]
    public void GetCurrentBranchShouldHandleTags()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("GITHUB_REF", "refs/tags/1.0.0");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/tags/1.0.0");
    }

    [Test]
    public void GetCurrentBranchShouldHandlePullRequests()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("GITHUB_REF", "refs/pull/1/merge");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/pull/1/merge");
    }

    [Test]
    public void GetSetParameterMessage()
    {
        // Assert
        this.environment.GetEnvironmentVariable("GitVersion_Something").ShouldBeNullOrWhiteSpace();

        // Act
        var result = this.buildServer.GenerateSetParameterMessage("GitVersion_Something", "1.0.0");

        // Assert
        result.ShouldContain(_ => true, 0);
    }

    [Test]
    public void SkipEmptySetParameterMessage()
    {
        // Act
        var result = this.buildServer.GenerateSetParameterMessage("Hello", string.Empty);

        // Assert
        result.ShouldBeEquivalentTo(Array.Empty<string>());
    }

    [Test]
    public void ShouldWriteIntegration()
    {
        // Arrange
        var vars = new TestableGitVersionVariables { Major = "1.0.0" };

        var list = new List<string?>();

        // Assert
        this.environment.GetEnvironmentVariable("GitVersion_Major").ShouldBeNullOrWhiteSpace();

        // Act
        this.buildServer.WriteIntegration(s => list.Add(s), vars);

        // Assert
        var expected = new List<string> { "Executing GenerateSetVersionMessage for 'GitHubActions'.", "", "Executing GenerateBuildLogOutput for 'GitHubActions'.", "Writing version variables to $GITHUB_ENV file for 'GitHubActions'." };

        string.Join(PathHelper.NewLine, list)
            .ShouldBe(string.Join(PathHelper.NewLine, expected));

        var expectedFileContents = new List<string> { "GitVersion_Major=1.0.0" };

        this.githubSetEnvironmentTempFilePath.ShouldNotBeNull();
        var actualFileContents = File.ReadAllLines(this.githubSetEnvironmentTempFilePath);

        actualFileContents.ShouldBe(expectedFileContents);
    }

    [Test]
    public void ShouldNotWriteIntegration()
    {
        // Arrange
        var vars = new TestableGitVersionVariables { FullSemVer = "1.0.0" };

        var list = new List<string?>();

        // Assert
        this.environment.GetEnvironmentVariable("GitVersion_Major").ShouldBeNullOrWhiteSpace();

        // Act
        this.buildServer.WriteIntegration(s => list.Add(s), vars, false);

        list.ShouldNotContain(x => x != null && x.StartsWith("Executing GenerateSetVersionMessage for "));
    }

    [Test]
    public void GetEmptyGenerateSetVersionMessage()
    {
        // Arrange
        var vars = new TestableGitVersionVariables { FullSemVer = "1.0.0" };

        // Act
        var message = this.buildServer.GenerateSetVersionMessage(vars);

        // Assert
        message.ShouldBeEmpty();
    }
}
