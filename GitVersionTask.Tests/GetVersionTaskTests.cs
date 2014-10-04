    using System;
    using System.Linq;
    using GitVersion;
    using GitVersionTask;
    using Microsoft.Build.Framework;
    using NUnit.Framework;

[TestFixture]
public class GetVersionTaskTests
{
    [Test]
    public void OutputsShouldMatchVariableProvider()
    {
        var taskType = typeof(GetVersion);
        var properties = taskType.GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(OutputAttribute), false).Any())
            .Select(p => p.Name);
        var variables = VariableProvider.GetVariablesFor(new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new SemanticVersionBuildMetaData(5, "develop", new ReleaseDate
            {
                OriginalCommitSha = "originalCommitSha",
                OriginalDate = DateTimeOffset.Parse("2014-03-01 00:00:01Z"),
            }, "commitSha",DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
        }).Keys;

        CollectionAssert.AreEquivalent(properties, variables);
    }
}
