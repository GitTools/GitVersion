using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests;

[TestFixture]
public class JsonVersionBuilderTests : TestBase
{
    [SetUp]
    public void Setup() => ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();

    [Test]
    public void Json()
    {
        var versionSourceSemVer = new SemanticVersion(1, 1, 2);
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 0,
            PreReleaseTag = "unstable4",
            BuildMetaData = new(versionSourceSemVer, "versionSourceSha", 5, "feature1", "commitSha", "commitShortSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"), 0)
        };

        var serviceProvider = ConfigureServices();

        var variableProvider = serviceProvider.GetRequiredService<IVariableProvider>();
        var variables = variableProvider.GetVariablesFor(semanticVersion, EmptyConfigurationBuilder.New.Build(), 0);
        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }
}
