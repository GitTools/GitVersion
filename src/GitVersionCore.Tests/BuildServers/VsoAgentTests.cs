using GitVersion;
using NUnit.Framework;

[TestFixture]
public class VsoAgentTests
{
    [Test]
    public void Develop_branch()
    {
        var versionBuilder = new VsoAgent();
        var vsVersion = versionBuilder.GenerateSetVersionMessage("0.0.0-Unstable4");
        //  Assert.AreEqual("##vso[task.setvariable variable=GitBuildNumber;]0.0.0-Unstable4", vsVersion);

        Assert.Null(vsVersion);
    }

    [Test]
    public void EscapeValues()
    {
        var versionBuilder = new VsoAgent();
        var vsVersion = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
        Assert.AreEqual("##vso[task.setvariable variable=GitVersion.Foo;]0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'", vsVersion[0]);
        
    }

}