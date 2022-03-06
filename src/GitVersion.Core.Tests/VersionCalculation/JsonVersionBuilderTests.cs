using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
public class JsonVersionBuilderTests : TestBase
{
    [SetUp]
    public void Setup() => ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();

    [Test]
    public void Json()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 0,
            PreReleaseTag = "unstable4",
            BuildMetaData = new SemanticVersionBuildMetaData("versionSourceSha", 5, "feature1", "commitSha", "commitShortSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"), 0)
        };

        var config = new TestEffectiveConfiguration();

        var sp = ConfigureServices();

        var variableProvider = sp.GetRequiredService<IVariableProvider>();
        var variables = variableProvider.GetVariablesFor(semanticVersion, config, false);
        var json = variables.ToString();
        json.ShouldMatchApproved(c => c.SubFolder("Approved"));
    }
}
