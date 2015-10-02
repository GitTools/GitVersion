using System;
using ApprovalTests;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;

[TestFixture]
public class JsonVersionBuilderTests
{
    [Test]
    public void Json()
    {
        var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "unstable4",
                BuildMetaData = new SemanticVersionBuildMetaData(5, "feature1", "commitSha",DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
            };

        var config = new TestEffectiveConfiguration();

        var variables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
        var json = JsonOutputFormatter.ToJson(variables);
        Approvals.Verify(json);
    }
}
