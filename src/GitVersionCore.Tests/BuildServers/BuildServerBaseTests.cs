using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using GitVersion.OutputVariables;
using GitVersion;
using GitVersion.Logging;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class BuildServerBaseTests  : TestBase
    {
        private IVariableProvider buildServer;
        private IServiceProvider sp;

        [SetUp]
        public void SetUp()
        {
            sp = ConfigureServices(services =>
            {
                services.AddSingleton<BuildServer>();
            });
            buildServer = sp.GetService<IVariableProvider>();
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
            var buildServer = sp.GetService<BuildServer>();
            buildServer.WriteIntegration(writes.Add, variables);

            writes[1].ShouldBe("1.2.3-beta.1+5");
        }

        private class BuildServer : BuildServerBase
        {
            protected override string EnvironmentVariable { get; }

            public BuildServer(IEnvironment environment, ILog log) : base(environment, log)
            {
            }

            public override bool CanApplyToCurrentContext()
            {
                throw new NotImplementedException();
            }

            public override string GenerateSetVersionMessage(VersionVariables variables)
            {
                return variables.FullSemVer;
            }

            public override string[] GenerateSetParameterMessage(string name, string value)
            {
                return new string[0];
            }
        }
    }
}
