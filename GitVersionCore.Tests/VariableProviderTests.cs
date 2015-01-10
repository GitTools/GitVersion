using System;
using ApprovalTests;
using GitVersion;
using NUnit.Framework;

[TestFixture]
public class VariableProviderTests
{
    [Test]
    public void ProvidesVariablesInContinuousDeliveryMode()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

        var vars = VariableProvider.GetVariablesFor(semVer, AssemblyVersioningScheme.MajorMinorPatch, VersioningMode.ContinuousDelivery);

        Approvals.Verify(JsonOutputFormatter.ToJson(vars));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentMode()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

        var vars = VariableProvider.GetVariablesFor(semVer, AssemblyVersioningScheme.MajorMinorPatch, VersioningMode.ContinuousDeployment);

        Approvals.Verify(JsonOutputFormatter.ToJson(vars));
    }
}