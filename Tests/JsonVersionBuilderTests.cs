using System;
using ApprovalTests;
using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class JsonVersionBuilderTests
{
    [Test]
    public void Json()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Feature,
                                  BranchName = "feature1",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Stability = Stability.Unstable,
                                                           PreReleasePartOne = 4,
                                                           Suffix = "a682956d",
                                                       }
                              };
        var json = semanticVersion.ToJson().Replace("\r\n", Environment.NewLine);
        Approvals.Verify(json);
    }

}