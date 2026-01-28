using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Agents.Tests;

[TestFixture]
public sealed class CodeBuildTests : TestBase
{
    private IEnvironment environment;
    private IFileSystem fileSystem;
    private IServiceProvider sp;
    private CodeBuild buildServer;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<CodeBuild>());
        this.environment = this.sp.GetRequiredService<IEnvironment>();
        this.fileSystem = this.sp.GetRequiredService<IFileSystem>();
        this.buildServer = this.sp.GetRequiredService<CodeBuild>();
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
        var assemblyLocation = FileSystemHelper.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        assemblyLocation.ShouldNotBeNull();
        var f = FileSystemHelper.Path.Combine(assemblyLocation, "codebuild_this_file_should_be_deleted.properties");

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
