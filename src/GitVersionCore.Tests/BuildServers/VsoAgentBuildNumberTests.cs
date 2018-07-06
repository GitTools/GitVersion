using System;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class VsoAgentBuildNumberTests : TestBase
{
    string key = "BUILD_BUILDNUMBER";
    string logPrefix = "##vso[build.updatebuildnumber]";
    VsoAgent versionBuilder = new VsoAgent();

    [TearDown]
    public void TearDownVsoAgentBuildNumberTest()
    {
        Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
    }


    [TestCase("$(GitVersion.FullSemVer)", "1.0.0", "1.0.0")]
    [TestCase("$(GITVERSION_FULLSEMVER)", "1.0.0", "1.0.0")]
    [TestCase("$(GitVersion.FullSemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    [TestCase("$(GITVERSION_FULLSEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    public void VsoAgentBuildNumberWithFullSemVer(string buildNumberFormat, string myFullSemVer, string expectedBuildNumber)
    {
        Environment.SetEnvironmentVariable(key, buildNumberFormat, EnvironmentVariableTarget.Process);
        var vars = new TestableVersionVariables(fullSemVer: myFullSemVer);
        var logMessage = versionBuilder.GenerateSetVersionMessage(vars);
        logMessage.ShouldBe(logPrefix + expectedBuildNumber);
    }


    [TestCase("$(GitVersion.SemVer)", "1.0.0", "1.0.0")]
    [TestCase("$(GITVERSION_SEMVER)", "1.0.0", "1.0.0")]
    [TestCase("$(GitVersion.SemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    [TestCase("$(GITVERSION_SEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    public void VsoAgentBuildNumberWithSemVer(string buildNumberFormat, string mySemVer, string expectedBuildNumber)
    {
        Environment.SetEnvironmentVariable(key, buildNumberFormat, EnvironmentVariableTarget.Process);
        var vars = new TestableVersionVariables(semVer: mySemVer);
        var logMessage = versionBuilder.GenerateSetVersionMessage(vars);
        logMessage.ShouldBe(logPrefix + expectedBuildNumber);
    }

}