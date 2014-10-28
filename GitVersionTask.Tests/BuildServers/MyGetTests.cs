using GitVersion;
using NUnit.Framework;

[TestFixture]
public class MyGetTests
{
    [Test]
    public void Develop_branch()
    {
        var authentication = new Authentication();
        var versionBuilder = new MyGet(authentication);
        var message = versionBuilder.GenerateSetVersionMessage("0.0.0-Unstable4");
        Assert.AreEqual("##teamcity[buildNumber '0.0.0-Unstable4']", message);
    }

    [Test]
    public void EscapeValues()
    {
        var authentication = new Authentication();
        var versionBuilder = new MyGet(authentication);
        var message = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
        Assert.AreEqual("##teamcity[setParameter name='GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", message[0]);
        Assert.AreEqual("##teamcity[setParameter name='system.GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", message[1]);
    }

}