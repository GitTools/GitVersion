namespace GitVersionCore.Tests
{
    using GitVersion;

    public class TestEffectiveConfiguration : EffectiveConfiguration
    {
        public TestEffectiveConfiguration(
            AssemblyVersioningScheme assemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch, 
            VersioningMode versioningMode = VersioningMode.ContinuousDelivery, 
            string gitTagPrefix = "v", 
            string tag = "",
            string nextVersion = null,
            string branchPrefixToTrim = "",
            bool preventIncrementForMergedBranchVersion = false,
            string tagNumberPattern = null,
            string continuousDeploymentFallbackTag = "ci",
            bool trackMergeTarget = false,
            string[] commitsToIgnore = null,
            string[] mergeMessagesToIgnore = null) : 
                base(assemblyVersioningScheme, versioningMode, gitTagPrefix, tag, nextVersion, IncrementStrategy.Patch, 
                    branchPrefixToTrim, preventIncrementForMergedBranchVersion, tagNumberPattern, continuousDeploymentFallbackTag,
                    trackMergeTarget, commitsToIgnore, mergeMessagesToIgnore)
        {
        }
    }
}