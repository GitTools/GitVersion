using System;
using ApprovalTests;
using GitVersion;
using NUnit.Framework;

[TestFixture]
public class PropertiesFileBuilderTests
{
    [Test]
    public void Props()
    {
        var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "unstable4",
                BuildMetaData = new SemanticVersionBuildMetaData(5, "feature1", "commitSha",DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
            };
        var variables = VariableProvider.GetVariablesFor(semanticVersion, AssemblyVersioningScheme.MajorMinorPatch, VersioningMode.ContinuousDelivery, "ci", false);
        var props = PropertiesOutputFormatter.ToProperties(variables);
        Approvals.Verify(props);
    }
}
