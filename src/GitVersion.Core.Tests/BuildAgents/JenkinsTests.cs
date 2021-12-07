using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class JenkinsTests : TestBase
{
    private const string key = "JENKINS_URL";
    private const string branch = "GIT_BRANCH";
    private const string localBranch = "GIT_LOCAL_BRANCH";
    private const string pipelineBranch = "BRANCH_NAME";
    private IEnvironment environment;
    private IServiceProvider sp;
    private Jenkins buildServer;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<Jenkins>());
        this.environment = this.sp.GetService<IEnvironment>();
        this.buildServer = this.sp.GetService<Jenkins>();
    }

    private void SetEnvironmentVariableForDetection() => this.environment.SetEnvironmentVariable(key, "a value");

    private void ClearenvironmentVariableForDetection() => this.environment.SetEnvironmentVariable(key, null);

    [Test]
    public void CanApplyCurrentContextWhenenvironmentVariableIsSet()
    {
        SetEnvironmentVariableForDetection();
        this.buildServer.CanApplyToCurrentContext().ShouldBe(true);
    }

    [Test]
    public void CanNotApplyCurrentContextWhenenvironmentVariableIsNotSet()
    {
        ClearenvironmentVariableForDetection();
        this.buildServer.CanApplyToCurrentContext().ShouldBe(false);
    }

    [Test]
    public void JenkinsTakesLocalBranchNameNotRemoteName()
    {
        // Save original values so they can be restored
        var branchOrig = this.environment.GetEnvironmentVariable(branch);
        var localBranchOrig = this.environment.GetEnvironmentVariable(localBranch);

        // Set GIT_BRANCH for testing
        this.environment.SetEnvironmentVariable(branch, $"origin/{MainBranch}");

        // Test Jenkins that GetCurrentBranch falls back to GIT_BRANCH if GIT_LOCAL_BRANCH undefined
        this.buildServer.GetCurrentBranch(true).ShouldBe($"origin/{MainBranch}");

        // Set GIT_LOCAL_BRANCH
        this.environment.SetEnvironmentVariable(localBranch, MainBranch);

        // Test Jenkins GetCurrentBranch method now returns GIT_LOCAL_BRANCH
        this.buildServer.GetCurrentBranch(true).ShouldBe(MainBranch);

        // Restore environment variables
        this.environment.SetEnvironmentVariable(branch, branchOrig);
        this.environment.SetEnvironmentVariable(localBranch, localBranchOrig);
    }

    [Test]
    public void JenkinsTakesBranchNameInPipelineAsCode()
    {
        // Save original values so they can be restored
        var branchOrig = this.environment.GetEnvironmentVariable(branch);
        var localBranchOrig = this.environment.GetEnvironmentVariable(localBranch);
        var pipelineBranchOrig = this.environment.GetEnvironmentVariable(pipelineBranch);

        // Set BRANCH_NAME in pipeline mode
        this.environment.SetEnvironmentVariable(pipelineBranch, MainBranch);
        // When Jenkins uses a Pipeline, GIT_BRANCH and GIT_LOCAL_BRANCH are not set:
        this.environment.SetEnvironmentVariable(branch, null);
        this.environment.SetEnvironmentVariable(localBranch, null);

        // Test Jenkins GetCurrentBranch method now returns BRANCH_NAME
        this.buildServer.GetCurrentBranch(true).ShouldBe(MainBranch);

        // Restore environment variables
        this.environment.SetEnvironmentVariable(branch, branchOrig);
        this.environment.SetEnvironmentVariable(localBranch, localBranchOrig);
        this.environment.SetEnvironmentVariable(pipelineBranch, pipelineBranchOrig);
    }

    [Test]
    public void GenerateSetVersionMessageReturnsVersionAsIsAlthoughThisIsNotUsedByJenkins()
    {
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
        this.buildServer.GenerateSetVersionMessage(vars).ShouldBe("0.0.0-Beta4.7");
    }

    [Test]
    public void GenerateMessageTest()
    {
        var generatedParameterMessages = this.buildServer.GenerateSetParameterMessage("name", "value");
        generatedParameterMessages.Length.ShouldBe(1);
        generatedParameterMessages[0].ShouldBe("GitVersion_name=value");
    }

    [Test]
    public void WriteAllVariablesToTheTextWriter()
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var f = Path.Combine(assemblyLocation, "gitlab_this_file_should_be_deleted.properties");

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
        var writes = new List<string>();
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "beta1",
            BuildMetaData = "5"
        };

        semanticVersion.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");
        semanticVersion.BuildMetaData.Sha = "commitSha";

        var config = new TestEffectiveConfiguration();

        var variableProvider = this.sp.GetService<IVariableProvider>();

        var variables = variableProvider.GetVariablesFor(semanticVersion, config, false);

        this.buildServer.WithPropertyFile(file);

        this.buildServer.WriteIntegration(writes.Add, variables);

        writes[1].ShouldBe("1.2.3-beta.1+5");

        File.Exists(file).ShouldBe(true);

        var props = File.ReadAllText(file);

        props.ShouldContain("GitVersion_Major=1");
        props.ShouldContain("GitVersion_Minor=2");
    }
}
