using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class BitBucketPipelinesTests : TestBase
{
    private IEnvironment environment;
    private BitBucketPipelines buildServer;
    private IServiceProvider sp;

    [SetUp]
    public void SetEnvironmentVariableForTest()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<BitBucketPipelines>());
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.buildServer = sp.GetRequiredService<BitBucketPipelines>();

        this.environment.SetEnvironmentVariable(BitBucketPipelines.EnvironmentVariableName, "MyWorkspace");
    }


    [Test]
    public void CanNotApplyToCurrentContextWhenEnvironmentVariableNotSet()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(BitBucketPipelines.EnvironmentVariableName, "");

        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void CalculateVersionOnMainBranch()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(BitBucketPipelines.BranchEnvironmentVariableName, "refs/heads/main");

        var vars = new TestableVersionVariables(fullSemVer: "1.2.3");
        var vsVersion = this.buildServer.GenerateSetVersionMessage(vars);

        vsVersion.ShouldBe("1.2.3");
    }

    [Test]
    public void CalculateVersionOnDevelopBranch()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(BitBucketPipelines.BranchEnvironmentVariableName, "refs/heads/develop");

        var vars = new TestableVersionVariables(fullSemVer: "1.2.3-unstable.4");
        var vsVersion = this.buildServer.GenerateSetVersionMessage(vars);

        vsVersion.ShouldBe("1.2.3-unstable.4");
    }

    [Test]
    public void CalculateVersionOnFeatureBranch()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(BitBucketPipelines.BranchEnvironmentVariableName, "refs/heads/feature/my-work");

        var vars = new TestableVersionVariables(fullSemVer: "1.2.3-beta.4");
        var vsVersion = this.buildServer.GenerateSetVersionMessage(vars);

        vsVersion.ShouldBe("1.2.3-beta.4");
    }

    [Test]
    public void GetCurrentBranchShouldHandleBranches()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(BitBucketPipelines.BranchEnvironmentVariableName, "refs/heads/feature/my-work");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe($"refs/heads/feature/my-work");
    }

    [Test]
    public void GetCurrentBranchShouldHandleTags()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(BitBucketPipelines.BranchEnvironmentVariableName, null);
        this.environment.SetEnvironmentVariable(BitBucketPipelines.TagEnvironmentVariableName, "refs/heads/tags/1.2.3");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void GetCurrentBranchShouldHandlePullRequests()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(BitBucketPipelines.BranchEnvironmentVariableName, null);
        this.environment.SetEnvironmentVariable(BitBucketPipelines.TagEnvironmentVariableName, null);
        this.environment.SetEnvironmentVariable(BitBucketPipelines.PullRequestEnvironmentVariableName, "refs/pull/1/merge");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBeNull();
    }


    [Test]
    public void WriteAllVariablesToTheTextWriter()
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        assemblyLocation.ShouldNotBeNull();
        var f = PathHelper.Combine(assemblyLocation, "gitversion.env");

        try
        {
            AssertVariablesAreWrittenToFile(f);
        }
        finally
        {
            File.Delete(f);
        }
    }

    private void AssertVariablesAreWrittenToFile(string file)
    {
        var writes = new List<string?>();
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "beta1",
            BuildMetaData = "5"
        };

        semanticVersion.BuildMetaData.CommitDate = new DateTimeOffset(2022, 4, 6, 16, 10, 59, TimeSpan.FromHours(10));
        semanticVersion.BuildMetaData.Sha = "f28807e615e9f06aec8a33c87780374e0c1f6fb8";

        var config = new TestEffectiveConfiguration();
        var variableProvider = this.sp.GetRequiredService<IVariableProvider>();

        var variables = variableProvider.GetVariablesFor(semanticVersion, config, false);

        this.buildServer.WithPropertyFile(file);

        this.buildServer.WriteIntegration(writes.Add, variables);

        writes[1].ShouldBe("1.2.3-beta.1+5");

        File.Exists(file).ShouldBe(true);

        var props = File.ReadAllText(file);

        props.ShouldContain("export GITVERSION_MAJOR=1");
        props.ShouldContain("export GITVERSION_MINOR=2");
        props.ShouldContain("export GITVERSION_SHA=f28807e615e9f06aec8a33c87780374e0c1f6fb8");
        props.ShouldContain("export GITVERSION_COMMITDATE=2022-04-06");
    }
}
