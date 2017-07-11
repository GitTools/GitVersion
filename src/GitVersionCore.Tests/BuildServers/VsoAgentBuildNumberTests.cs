using System;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;

[TestFixture]
public class VsoAgentBuildNumberTests
{
    string key = "BUILD_BUILDNUMBER";
    string logPrefix = "##vso[build.updatebuildnumber]";
    List<Tuple<string, string, string>> examples;
    VsoAgent versionBuilder = new VsoAgent();

    [SetUp]
    public void SetUpVsoAgentBuildNumberTest()
    {
         examples = new List<Tuple<string, string, string>>();
    }

    [TearDown]
    public void TearDownVsoAgentBuildNumberTest()
    {
        examples = null;
        Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
    }


    [Test]
    public void VsoAgentBuildNumberWithFullSemVer()
    {
        examples.Add(new Tuple<string, string, string>("$(GitVersion.FullSemVer)", "1.0.0", "1.0.0"));
        examples.Add(new Tuple<string, string, string>("$(GITVERSION_FULLSEMVER)", "1.0.0", "1.0.0"));
        examples.Add(new Tuple<string, string, string>("$(GitVersion.FullSemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234"));
        examples.Add(new Tuple<string, string, string>("$(GITVERSION_FULLSEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234"));

        foreach (var example in examples)
        {
            Environment.SetEnvironmentVariable(key, example.Item1, EnvironmentVariableTarget.Process);
            var vars = new TestableVersionVariables(fullSemVer: example.Item2);

            var logMessage = versionBuilder.GenerateSetVersionMessage(vars);
            logMessage.ShouldBe(logPrefix + example.Item3);
        }
    }


    [Test]
    public void VsoAgentBuildNumberWithSemVer()
    {
        examples.Add(new Tuple<string, string, string>("$(GitVersion.SemVer)", "1.0.0", "1.0.0"));
        examples.Add(new Tuple<string, string, string>("$(GITVERSION_SEMVER)", "1.0.0", "1.0.0"));
        examples.Add(new Tuple<string, string, string>("$(GitVersion.SemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234"));
        examples.Add(new Tuple<string, string, string>("$(GITVERSION_SEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234"));

        foreach (var example in examples)
        {
            Environment.SetEnvironmentVariable(key, example.Item1, EnvironmentVariableTarget.Process);
            var vars = new TestableVersionVariables(semVer: example.Item2);

            var logMessage = versionBuilder.GenerateSetVersionMessage(vars);
            logMessage.ShouldBe(logPrefix + example.Item3);
        }
    }

}