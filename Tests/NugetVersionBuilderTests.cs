using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class NugetVersionBuilderTests
{

    [TestCase(BranchType.Develop, null, null, "unstable4", null, null,
              "0.0.0-Unstable0004")]
    [TestCase(BranchType.Develop, null, null, "unstable4", null, 6,
              "0.0.0-Unstable0004-0006")]
    [TestCase(BranchType.Release, null, null, "beta4", null, null,
              "0.0.0-Beta0004")]
    [TestCase(BranchType.Release, null, null, "beta4", null, 8,
              "0.0.0-Beta0004-0008")]
    [TestCase(BranchType.Hotfix, null, null, "beta4", null, null,
              "0.0.0-Beta0004")]
    [TestCase(BranchType.Hotfix, null, null, "beta4", null, 7,
              "0.0.0-Beta0004-0007")]
    [TestCase(BranchType.PullRequest, null, null, "unstable131231232", "1571", 131231232,
              "0.0.0-PullRequest-1571")]
    [TestCase(BranchType.Feature, "AFeature", "TheSha", "unstable4", null, null,
              "0.0.0-Feature-AFeature-TheSha")]
    [TestCase(BranchType.Master, null, null, null, "1571", 131231232,
              "0.0.0")]
    [TestCase(BranchType.Develop, null, null, "unstable4", null, null,
              "0.0.0-Unstable0004")]
    [TestCase(BranchType.Develop, null, null, "unstable40", null, 50,
              "0.0.0-Unstable0040-0050")]
    [TestCase(BranchType.Develop, null, null, "unstable400", null, null,
              "0.0.0-Unstable0400")]
    [TestCase(BranchType.Develop, null, null, "unstable400", null, 500,
              "0.0.0-Unstable0400-0500")]
    [TestCase(BranchType.Develop, null, null, "unstable4000", null, null,
              "0.0.0-Unstable4000")]
    [TestCase(BranchType.Develop, null, null, "unstable4000", null, 4000,
              "0.0.0-Unstable4000-4000")]
    public void ValidateNuGetVersionBuilder(BranchType branchType, string branchName, string sha,
        string tag, string suffix, int? preReleasePartTwo, string versionString)
    {
        var versionAndBranch = new VersionAndBranch
        {
            BranchType = branchType,
            BranchName = branchName,
            Sha = sha,
            Version = new SemanticVersion
            {
                PreReleasePartTwo = preReleasePartTwo,
                Suffix = suffix,
                Tag = tag
            }
        };
        var nugetVersion = versionAndBranch.GenerateNugetVersion();
        NuGet.SemanticVersion.Parse(nugetVersion);
        Assert.AreEqual(versionString, nugetVersion);

    }
}