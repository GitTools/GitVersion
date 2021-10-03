using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public sealed class CodeBuildTests : TestBase
{
    private IEnvironment environment;
    private IServiceProvider sp;
    private CodeBuild buildServer;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<CodeBuild>());
        this.environment = this.sp.GetService<IEnvironment>();
        this.buildServer = this.sp.GetService<CodeBuild>();
    }

    [Test]
    public void CorrectlyIdentifiesCodeBuildPresenceFromSourceVersion()
    {
        this.environment.SetEnvironmentVariable(CodeBuild.SourceVersionEnvironmentVariableName, "a value");
        this.buildServer.CanApplyToCurrentContext().ShouldBe(true);
    }

    [Test]
    public void PicksUpBranchNameFromEnvironmentFromSourceVersion()
    {
        this.environment.SetEnvironmentVariable(CodeBuild.SourceVersionEnvironmentVariableName, $"refs/heads/{MainBranch}");
        this.buildServer.GetCurrentBranch(false).ShouldBe($"refs/heads/{MainBranch}");
    }

    [Test]
    public void CorrectlyIdentifiesCodeBuildPresenceFromWebHook()
    {
        this.environment.SetEnvironmentVariable(CodeBuild.WebHookEnvironmentVariableName, "a value");
        this.buildServer.CanApplyToCurrentContext().ShouldBe(true);
    }

    [Test]
    public void PicksUpBranchNameFromEnvironmentFromWebHook()
    {
        this.environment.SetEnvironmentVariable(CodeBuild.WebHookEnvironmentVariableName, $"refs/heads/{MainBranch}");
        this.buildServer.GetCurrentBranch(false).ShouldBe($"refs/heads/{MainBranch}");
    }

    [Test]
    public void WriteAllVariablesToTheTextWriter()
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var f = Path.Combine(assemblyLocation, "codebuild_this_file_should_be_deleted.properties");

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
