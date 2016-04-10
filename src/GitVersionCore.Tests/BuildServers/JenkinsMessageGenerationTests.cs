using System;
using System.Collections.Generic;
using System.IO;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class JenkinsMessageGenerationTests
{
    [Test]
    public void GenerateSetVersionMessageReturnsVersionAsIs_AlthoughThisIsNotUsedByJenkins()
    {
        var j = new Jenkins();
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
        j.GenerateSetVersionMessage(vars).ShouldBe("0.0.0-Beta4.7");
    }

    [Test]
    public void GenerateMessageTest()
    {
        var j = new Jenkins();
        var generatedParameterMessages = j.GenerateSetParameterMessage("name", "value");
        generatedParameterMessages.Length.ShouldBe(1);
        generatedParameterMessages[0].ShouldBe("GitVersion_name=value");
    }

    [Test]
    public void WriteAllVariablesToTheTextWriter()
    {
        // this test method writes to disc, hence marked explicit
        var f = "this_file_should_be_deleted.properties";

        try
        {
            AssertVariablesAreWrittenToFile(f);
        }
        finally
        {
            File.Delete(f);
        }
    }

    static void AssertVariablesAreWrittenToFile(string f)
    {
        var writes = new List<string>();
        var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "beta1",
                BuildMetaData = "5"
            };

        semanticVersion.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");
        semanticVersion.BuildMetaData.Sha = "commitSha";

        var config = new TestEffectiveConfiguration();

        var variables = VariableProvider.GetVariablesFor(semanticVersion, config, false);

        var j = new Jenkins(f);

        j.WriteIntegration(writes.Add, variables);

        writes[1].ShouldBe("1.2.3-beta.1+5");

        File.Exists(f).ShouldBe(true);

        var props = File.ReadAllText(f);

        props.ShouldContain("GitVersion_Major=1");
        props.ShouldContain("GitVersion_Minor=2");
    }
}