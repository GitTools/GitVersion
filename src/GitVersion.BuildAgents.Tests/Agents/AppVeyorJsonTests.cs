using GitVersion.Agents;

namespace GitVersion.BuildAgents.Tests;

[TestFixture]
public class AppVeyorJsonTests
{
    [Test]
    public void BuildNumberRequestPreservesWireNameAndVersion()
    {
        var body = new AppVeyorBuildNumberRequest("1.2.3-alpha.4.build.42");

        var json = JsonSerializer.Serialize(body, AppVeyorJsonContext.Default.AppVeyorBuildNumberRequest);

        json.ShouldBe("""{"version":"1.2.3-alpha.4.build.42"}""");
    }

    [TestCase("", """{"name":"GitVersion_FullSemVer","value":""}""")]
    [TestCase("1.2.3", """{"name":"GitVersion_FullSemVer","value":"1.2.3"}""")]
    [TestCase("quoted \"value\"\n", """{"name":"GitVersion_FullSemVer","value":"quoted \u0022value\u0022\n"}""")]
    public void VariableRequestPreservesWireNamesAndEscaping(string value, string expected)
    {
        var body = new AppVeyorVariableRequest("GitVersion_FullSemVer", value);

        var json = JsonSerializer.Serialize(body, AppVeyorJsonContext.Default.AppVeyorVariableRequest);

        json.ShouldBe(expected);
    }
}
