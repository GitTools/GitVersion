using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.BuildAgents.Tests;

[TestFixture]
public class GitLabCiTests : TestBase
{
    private IEnvironment environment;
    private IFileSystem fileSystem;
    private IServiceProvider sp;
    private GitLabCi buildServer;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<GitLabCi>());
        this.environment = this.sp.GetRequiredService<IEnvironment>();
        this.fileSystem = this.sp.GetRequiredService<IFileSystem>();
        this.buildServer = this.sp.GetRequiredService<GitLabCi>();
        this.environment.SetEnvironmentVariable(GitLabCi.EnvironmentVariableName, "true");
    }

    [TearDown]
    public void TearDown() => this.environment.SetEnvironmentVariable(GitLabCi.EnvironmentVariableName, null);

    [Test]
    public void ShouldSetBuildNumber()
    {
        var vars = new TestableGitVersionVariables { FullSemVer = "0.0.0-Beta4.7" };
        this.buildServer.SetBuildNumber(vars).ShouldBe("0.0.0-Beta4.7");
    }

    [Test]
    public void ShouldSetOutputVariables()
    {
        var result = this.buildServer.SetOutputVariables("name", "value");
        result.Length.ShouldBe(1);
        result[0].ShouldBe("GitVersion_name=value");
    }

    [TestCase("main", "main")]
    [TestCase("dev", "dev")]
    [TestCase("development", "development")]
    [TestCase("my_cool_feature", "my_cool_feature")]
    [TestCase("#3-change_projectname", "#3-change_projectname")]
    public void GetCurrentBranchShouldHandleBranches(string branchName, string expectedResult)
    {
        this.environment.SetEnvironmentVariable("CI_COMMIT_REF_NAME", branchName);

        var result = this.buildServer.GetCurrentBranch(false);

        result.ShouldBe(expectedResult);
    }

    [TestCase("main", "", "main")]
    [TestCase("v1.0.0", "v1.0.0", null)]
    [TestCase("development", "", "development")]
    [TestCase("v1.2.1", "v1.2.1", null)]
    public void GetCurrentBranchShouldHandleTags(string branchName, string commitTag, string? expectedResult)
    {
        this.environment.SetEnvironmentVariable("CI_COMMIT_REF_NAME", branchName);
        this.environment.SetEnvironmentVariable("CI_COMMIT_TAG", commitTag); // only set in pipelines for tags

        var result = this.buildServer.GetCurrentBranch(false);

        if (!string.IsNullOrEmpty(expectedResult))
        {
            result.ShouldBe(expectedResult);
        }
        else
        {
            result.ShouldBeNull();
        }
    }

    [TestCase("main", "main")]
    [TestCase("dev", "dev")]
    [TestCase("development", "development")]
    [TestCase("my_cool_feature", "my_cool_feature")]
    [TestCase("#3-change_projectname", "#3-change_projectname")]
    public void GetCurrentBranchShouldHandlePullRequests(string branchName, string expectedResult)
    {
        this.environment.SetEnvironmentVariable("CI_COMMIT_REF_NAME", branchName);

        var result = this.buildServer.GetCurrentBranch(false);

        result.ShouldBe(expectedResult);
    }

    [Test]
    public void WriteAllVariablesToTheTextWriter()
    {
        var assemblyLocation = FileSystemHelper.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        assemblyLocation.ShouldNotBeNull();
        var f = FileSystemHelper.Path.Combine(assemblyLocation, "jenkins_this_file_should_be_deleted.properties");

        try
        {
            AssertVariablesAreWrittenToFile(f);
        }
        finally
        {
            this.fileSystem.File.Delete(f);
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
            BuildMetaData = new SemanticVersionBuildMetaData("5")
            {
                Sha = "commitSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var variableProvider = this.sp.GetRequiredService<IVariableProvider>();

        var variables = variableProvider.GetVariablesFor(semanticVersion, EmptyConfigurationBuilder.New.Build(), 0);

        this.buildServer.WithPropertyFile(file);

        this.buildServer.WriteIntegration(writes.Add, variables);

        writes[1].ShouldBe("1.2.3-beta.1+5");

        this.fileSystem.File.Exists(file).ShouldBe(true);

        var props = this.fileSystem.File.ReadAllText(file);

        props.ShouldContain("GitVersion_Major=1");
        props.ShouldContain("GitVersion_Minor=2");
    }
}
