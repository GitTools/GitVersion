using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class BitBucketPipelinesTests : TestBase
{
    private IEnvironment environment;
    private BitBucketPipelines buildServer;

    [SetUp]
    public void SetEnvironmentVariableForTest()
    {
        var sp = ConfigureServices(services => services.AddSingleton<BitBucketPipelines>());
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.buildServer = sp.GetRequiredService<BitBucketPipelines>();

        this.environment.SetEnvironmentVariable("BITBUCKET_WORKSPACE", "MyWorkspace");
    }

    [Test]
    public void CalculateVersionOnMainBranch()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BITBUCKET_BRANCH", "refs/heads/main");

        var vars = new TestableVersionVariables(fullSemVer: "1.2.3");
        var vsVersion = this.buildServer.GenerateSetVersionMessage(vars);

        vsVersion.ShouldBe("1.2.3");
    }

    [Test]
    public void CalculateVersionOnDevelopBranch()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BITBUCKET_BRANCH", "refs/heads/develop");

        var vars = new TestableVersionVariables(fullSemVer: "1.2.3-unstable.4");
        var vsVersion = this.buildServer.GenerateSetVersionMessage(vars);

        vsVersion.ShouldBe("1.2.3-unstable.4");
    }

    [Test]
    public void CalculateVersionOnFeatureBranch()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BITBUCKET_BRANCH", "refs/heads/feature/my-work");

        var vars = new TestableVersionVariables(fullSemVer: "1.2.3-beta.4");
        var vsVersion = this.buildServer.GenerateSetVersionMessage(vars);

        vsVersion.ShouldBe("1.2.3-beta.4");
    }

    [Test]
    public void GetCurrentBranchShouldHandleBranches()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BITBUCKET_BRANCH", "refs/heads/feature/my-work");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe($"refs/heads/feature/my-work");
    }

    [Test]
    public void GetCurrentBranchShouldHandleTags()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BITBUCKET_BRANCH", null);
        this.environment.SetEnvironmentVariable("BITBUCKET_TAG", "refs/heads/tags/1.2.3");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void GetCurrentBranchShouldHandlePullRequests()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BITBUCKET_BRANCH", null);
        this.environment.SetEnvironmentVariable("BITBUCKET_TAG", null);
        this.environment.SetEnvironmentVariable("BITBUCKET_PR_ID", "refs/pull/1/merge");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBeNull();
    }
}
