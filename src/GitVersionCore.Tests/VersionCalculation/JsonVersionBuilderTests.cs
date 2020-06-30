using System;
using GitVersion;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

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

            var sp = ConfigureServices();

            var variableProvider = sp.GetService<IVariableProvider>();
            var variables = variableProvider.GetVariablesFor(semanticVersion, config, false);
            var json = variables.ToString();
            json.ShouldMatchApproved(c => c.SubFolder("Approved"));
        }
    }
}
