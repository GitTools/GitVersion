using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class ContinuaCiTests
{

    [Test]
    public void GenerateBuildVersion()
    {
        var versionBuilder = new ContinuaCi();
        var continuaCiVersion = versionBuilder.GenerateSetVersionMessage("0.0.0-Beta4.7");
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.7']", continuaCiVersion);
    }

}