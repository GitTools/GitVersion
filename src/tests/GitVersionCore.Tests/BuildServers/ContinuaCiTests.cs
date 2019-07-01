using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;

[TestFixture]
public class ContinuaCiTests : TestBase
{

    [Test]
    public void GenerateBuildVersion()
    {
        var versionBuilder = new ContinuaCi();
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
        var continuaCiVersion = versionBuilder.GenerateSetVersionMessage(vars);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.7']", continuaCiVersion);
    }

}