using System;
using System.Collections.Generic;
using GitVersion;
using NUnit.Framework;
using Shouldly;
using GitVersion.OutputVariables;
using GitVersion.Common;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class BuildServerBaseTests  : TestBase
    {

        private IEnvironment environment;

        [SetUp]
        public void SetUp()
        {
            environment = new TestEnvironment();
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

            var variables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
            new BuildServer(environment).WriteIntegration(writes.Add, variables);

            writes[1].ShouldBe("1.2.3-beta.1+5");
        }

        class BuildServer : BuildServerBase
        {
            protected override string EnvironmentVariable { get; }

            public BuildServer(IEnvironment environment) : base(environment)
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
