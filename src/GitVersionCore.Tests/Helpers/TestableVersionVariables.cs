using GitVersion.OutputVariables;

namespace GitVersionCore.Tests.Helpers
{
    internal class TestableVersionVariables : VersionVariables
    {
        public TestableVersionVariables(
            string major = "", string minor = "", string patch = "", string buildMetaData = "",
            string buildMetaDataPadded = "", string fullBuildMetaData = "", string branchName = "",
            string escapedBranchName = "", string sha = "", string shortSha = "", string majorMinorPatch = "",
            string semVer = "", string legacySemVer = "", string legacySemVerPadded = "", string fullSemVer = "",
            string assemblySemVer = "", string assemblySemFileVer = "", string preReleaseTag = "",
            string preReleaseTagWithDash = "", string preReleaseLabel = "", string preReleaseNumber = "",
            string weightedPreReleaseNumber = "", string informationalVersion = "", string commitDate = "",
            string nugetVersion = "", string nugetVersionV2 = "", string nugetPreReleaseTag = "",
            string nugetPreReleaseTagV2 = "", string versionSourceSha = "", string commitsSinceVersionSource = "",
            string commitsSinceVersionSourcePadded = "", string uncommittedChanges = "") : base(
                major, minor, patch, buildMetaData, buildMetaDataPadded, fullBuildMetaData, branchName, escapedBranchName,
                sha, shortSha, majorMinorPatch, semVer, legacySemVer, legacySemVerPadded, fullSemVer,
                assemblySemVer, assemblySemFileVer, preReleaseTag, weightedPreReleaseNumber, preReleaseTagWithDash,
                preReleaseLabel, preReleaseNumber, informationalVersion, commitDate, nugetVersion, nugetVersionV2,
                nugetPreReleaseTag, nugetPreReleaseTagV2, versionSourceSha, commitsSinceVersionSource, commitsSinceVersionSourcePadded, uncommittedChanges)
        {
        }
    }
}
