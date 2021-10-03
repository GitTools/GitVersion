using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class BuildServerBaseTests : TestBase
{
    private IVariableProvider buildServer;
    private IServiceProvider sp;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<BuildAgent>());
        this.buildServer = this.sp.GetService<IVariableProvider>();
    }

    [Test]
    public void BuildNumberIsFullSemVer()
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

        var variables = this.buildServer.GetVariablesFor(semanticVersion, config, false);
        var buildServer = this.sp.GetService<BuildAgent>();
        buildServer.WriteIntegration(writes.Add, variables);

        writes[1].ShouldBe("1.2.3-beta.1+5");

        writes = new List<string>();
        buildServer.WriteIntegration(writes.Add, variables, false);
        writes.ShouldNotContain(x => x.StartsWith("Executing GenerateSetVersionMessage for "));
    }

    private class BuildAgent : BuildAgentBase
    {
        protected override string EnvironmentVariable { get; }

        public BuildAgent(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public override bool CanApplyToCurrentContext() => throw new NotImplementedException();

        public override string GenerateSetVersionMessage(VersionVariables variables) => variables.FullSemVer;

        public override string[] GenerateSetParameterMessage(string name, string value) => Array.Empty<string>();
    }
}
