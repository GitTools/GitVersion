namespace GitVersionCore.Tests
{
    using GitVersion;

    public class TestEffectiveConfiguration : EffectiveConfiguration
    {
        public TestEffectiveConfiguration(
            AssemblyVersioningScheme assemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch, 
            string assemblyInformationalFormat = null,
            VersioningMode versioningMode = VersioningMode.ContinuousDelivery, 
            string gitTagPrefix = "v", 
            string tag = "",
            string nextVersion = null,
            string branchPrefixToTrim = "",
            bool preventIncrementForMergedBranchVersion = false,
            string tagNumberPattern = null,
            string continuousDeploymentFallbackTag = "ci",
            bool trackMergeTarget = false,
            string majorMessage = null,
            string minorMessage = null,
            string patchMessage = null,
            CommitMessageIncrementMode commitMessageMode = CommitMessageIncrementMode.Enabled,
            int legacySemVerPadding = 4,
            int buildMetaDataPadding = 4,
            int commitsSinceVersionSourcePadding = 4,
            bool isDevelop = false,
            bool isRelease = false) : 
                base(assemblyVersioningScheme, assemblyInformationalFormat, versioningMode, gitTagPrefix, tag, nextVersion, IncrementStrategy.Patch, 
                    branchPrefixToTrim, preventIncrementForMergedBranchVersion, tagNumberPattern, continuousDeploymentFallbackTag,
                    trackMergeTarget,
                    majorMessage, minorMessage, patchMessage,
                    commitMessageMode, legacySemVerPadding, buildMetaDataPadding, commitsSinceVersionSourcePadding,
                    isDevelop, isRelease)
        {
        }
    }
}