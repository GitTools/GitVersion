using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class VsoAgentTests
{
    [Test]
    public void Develop_branch()
    {
        var versionBuilder = new VsoAgent();
        var vsVersion = versionBuilder.GenerateSetVersionMessage("0.0.0-Unstable4");
        vsVersion.ShouldBe("##vso[build.updatebuildnumber]0.0.0-Unstable4");
    }

    [Test]
    public void EscapeValues()
    {
        var versionBuilder = new VsoAgent();
        var vsVersion = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");

        vsVersion[0].ShouldBe("##vso[task.setvariable variable=GitVersion.Foo;]0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
    }

}