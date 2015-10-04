namespace GitVersion
{
    /// <summary>
    /// Configuration can be applied to different things, effective configuration is the result after applying the appropriate configuration
    /// </summary>
    public class EffectiveConfiguration
    {
        public EffectiveConfiguration(
            AssemblyVersioningScheme assemblyVersioningScheme, 
            AssemblyInformationalVersioningScheme assemblyInformationalVersioningScheme,
            VersioningMode versioningMode, string gitTagPrefix, 
            string tag, string nextVersion, IncrementStrategy increment, 
            string branchPrefixToTrim, 
            bool preventIncrementForMergedBranchVersion, 
            string tagNumberPattern,
            string continuousDeploymentFallbackTag, 
            bool trackMergeTarget,
            string majorVersionBumpMessage,
            string minorVersionBumpMessage,
            string patchVersionBumpMessage,
            CommitMessageIncrementMode commitMessageIncrementing,
            int legacySemVerPaddding,
            int buildMetaDataPadding
            )
        {
            AssemblyVersioningScheme = assemblyVersioningScheme;
            AssemblyInformationalVersioningScheme = assemblyInformationalVersioningScheme;
            VersioningMode = versioningMode;
            GitTagPrefix = gitTagPrefix;
            Tag = tag;
            NextVersion = nextVersion;
            Increment = increment;
            BranchPrefixToTrim = branchPrefixToTrim;
            PreventIncrementForMergedBranchVersion = preventIncrementForMergedBranchVersion;
            TagNumberPattern = tagNumberPattern;
            ContinuousDeploymentFallbackTag = continuousDeploymentFallbackTag;
            TrackMergeTarget = trackMergeTarget;
            MajorVersionBumpMessage = majorVersionBumpMessage;
            MinorVersionBumpMessage = minorVersionBumpMessage;
            PatchVersionBumpMessage = patchVersionBumpMessage;
            CommitMessageIncrementing = commitMessageIncrementing;
            LegacySemVerPadding = legacySemVerPaddding;
            BuildMetaDataPadding = buildMetaDataPadding;
        }

        public VersioningMode VersioningMode { get; private set; }

        public AssemblyVersioningScheme AssemblyVersioningScheme { get; private set; }
        public AssemblyInformationalVersioningScheme AssemblyInformationalVersioningScheme { get; private set; }

        /// <summary>
        /// Git tag prefix
        /// </summary>
        public string GitTagPrefix { get; private set; }

        /// <summary>
        /// Tag to use when calculating SemVer
        /// </summary>
        public string Tag { get; private set; }

        public string NextVersion { get; private set; }

        public IncrementStrategy Increment { get; private set; }

        public string BranchPrefixToTrim { get; private set; }

        public bool PreventIncrementForMergedBranchVersion { get; private set; }

        public string TagNumberPattern { get; private set; }

        public string ContinuousDeploymentFallbackTag { get; private set; }

        public bool TrackMergeTarget { get; private set; }

        public string MajorVersionBumpMessage { get; private set; }
        
        public string MinorVersionBumpMessage { get; private set; }

        public string PatchVersionBumpMessage { get; private set; }

        public int LegacySemVerPadding { get; private set; }
        public  int BuildMetaDataPadding { get; private set; }

        public CommitMessageIncrementMode CommitMessageIncrementing { get; private set; }
    }
}