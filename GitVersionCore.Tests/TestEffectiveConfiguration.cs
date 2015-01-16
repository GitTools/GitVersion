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
            string branchPrefixToTrim = "") : 
                base(assemblyVersioningScheme, versioningMode, gitTagPrefix, tag, nextVersion, IncrementStrategy.Patch, branchPrefixToTrim)
        {
        }
    }
}