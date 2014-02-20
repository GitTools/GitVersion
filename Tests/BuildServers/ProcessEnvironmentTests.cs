using GitFlowVersion.BuildServers;
using NUnit.Framework;

[TestFixture]
public class ProcessEnvironmentTests
{
    [Test]
    public void GenerateBuildVersion()
    {
        var versionBuilder = new ProcessEnvironment();
        var environmentVersion = versionBuilder.GenerateSetVersionMessage("0.0.0-Beta4.7");
        Assert.AreEqual("GitFlowVersion = '0.0.0-Beta4.7'", environmentVersion);
    }

    [Test]
    public void GenerateVariable()
    {
        var versionBuilder = new ProcessEnvironment();
        var environmentVersion = versionBuilder.GenerateSetParameterMessage("AnyName", "0.0.0-Beta4.7");
        Assert.AreEqual("GitFlowVersion.AnyName = '0.0.0-Beta4.7'", environmentVersion);
    }
}
