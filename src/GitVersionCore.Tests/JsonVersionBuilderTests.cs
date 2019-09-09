using System;
using GitVersion;
using NUnit.Framework;
using Shouldly;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class JsonVersionBuilderTests : TestBase
    {
        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void Json()
        {
            var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 0,
                PreReleaseTag = "unstable4",
                BuildMetaData = new SemanticVersionBuildMetaData("versionSourceSha", 5, "feature1", "commitSha", "commitShortSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
            };

            var config = new TestEffectiveConfiguration();

            var variables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
            var json = JsonOutputFormatter.ToJson(variables);
            json.ShouldMatchApproved(c => c.SubFolder("Approved"));
        }
    }
}
