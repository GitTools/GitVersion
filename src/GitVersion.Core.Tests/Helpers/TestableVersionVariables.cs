using GitVersion.OutputVariables;

namespace GitVersion.Core.Tests.Helpers;

internal class TestableVersionVariables : VersionVariables
{
    public TestableVersionVariables(
        string major = "", string minor = "", string patch = "", string buildMetaData = "", string fullBuildMetaData = "", string branchName = "",
        string escapedBranchName = "", string sha = "", string shortSha = "", string majorMinorPatch = "",
        string semVer = "", string fullSemVer = "",
        string assemblySemVer = "", string assemblySemFileVer = "", string preReleaseTag = "",
        string preReleaseTagWithDash = "", string preReleaseLabel = "", string preReleaseLabelWithDash = "", string preReleaseNumber = "",
        string weightedPreReleaseNumber = "", string informationalVersion = "", string commitDate = "", string versionSourceSha = "", string commitsSinceVersionSource = "", string uncommittedChanges = "") : base(
        major, minor, patch, buildMetaData, fullBuildMetaData, branchName, escapedBranchName,
        sha, shortSha, majorMinorPatch, semVer, fullSemVer,
        assemblySemVer, assemblySemFileVer, preReleaseTag, weightedPreReleaseNumber, preReleaseTagWithDash,
        preReleaseLabel, preReleaseLabelWithDash, preReleaseNumber, informationalVersion, commitDate,
        versionSourceSha, commitsSinceVersionSource, uncommittedChanges)
    {
    }
}
