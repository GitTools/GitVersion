using GitVersion;
using NUnit.Framework;

[TestFixture]
public class ContinuaCiTests
{

    [Test]
    public void GenerateBuildVersion()
    {
        var arguments = new Arguments();
        var versionBuilder = new ContinuaCi(arguments);
        var continuaCiVersion = versionBuilder.GenerateSetVersionMessage("0.0.0-Beta4.7");
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.7']", continuaCiVersion);
    }

}