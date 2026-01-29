using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;

namespace GitVersion.BuildAgents.Tests;

[TestFixture]
public class BuildServerBaseTests : TestBase
{
    private IVariableProvider buildServer;
    private IServiceProvider sp;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<BuildAgent>());
        this.buildServer = this.sp.GetRequiredService<IVariableProvider>();
    }

    [Test]
    public void BuildNumberIsFullSemVer()
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

        var variables = this.buildServer.GetVariablesFor(semanticVersion, EmptyConfigurationBuilder.New.Build(), 0);
        var buildAgent = this.sp.GetRequiredService<BuildAgent>();
        buildAgent.WriteIntegration(writes.Add, variables);

        writes[1].ShouldBe("1.2.3-beta.1+5");

        writes = [];
        buildAgent.WriteIntegration(writes.Add, variables, false);
        writes.ShouldNotContain(x => x != null && x.StartsWith("Set Build Number for "));
    }

    private sealed class BuildAgent(IEnvironment environment, ILogger<BuildAgent> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
    {
        protected override string EnvironmentVariable => throw new NotImplementedException();

        public override bool CanApplyToCurrentContext() => throw new NotImplementedException();

        public override string SetBuildNumber(GitVersionVariables variables) => variables.FullSemVer;

        public override string[] SetOutputVariables(string name, string? value) => [];
    }
}
