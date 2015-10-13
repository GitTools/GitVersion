using System;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class VsoAgentTests
{

    string key = "BUILD_BUILDNUMBER";

    private void SetEnvironmentVariableForTest()
    {
        Environment.SetEnvironmentVariable(key, "Some Build_Value $(GitVersion_FullSemVer)", EnvironmentVariableTarget.Process);
    }

    private void ClearEnvironmentVariableForTest()
    {
        Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
    }

    [Test]
    public void Develop_branch()
    {
        SetEnvironmentVariableForTest();
        var versionBuilder = new VsoAgent();
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Unstable4");
        var vsVersion = versionBuilder.GenerateSetVersionMessage(vars);
        ClearEnvironmentVariableForTest();

        vsVersion.ShouldBe("##vso[build.updatebuildnumber]Some Build_Value 0.0.0-Unstable4");
    }

    [Test]
    public void EscapeValues()
    {
        var versionBuilder = new VsoAgent();
        var vsVersion = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");

        vsVersion[0].ShouldBe("##vso[task.setvariable variable=GitVersion.Foo;]0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
    }

}