using System.Collections.Generic;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Model.Configuration
{
    /// <summary>
    /// Configuration can be applied to different things, effective configuration is the result after applying the appropriate configuration
    /// </summary>
    public class EffectiveConfiguration
    {
        public EffectiveConfiguration(
            AssemblyVersioningScheme assemblyVersioningScheme,
            AssemblyFileVersioningScheme assemblyFileVersioningScheme,
            string assemblyInformationalFormat,
            string assemblyVersioningFormat,
            string assemblyFileVersioningFormat,
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
            string noBumpMessage,
            CommitMessageIncrementMode commitMessageIncrementing,
            int legacySemVerPaddding,
            int buildMetaDataPadding,
            int commitsSinceVersionSourcePadding,
            IEnumerable<IVersionFilter> versionFilters,
            bool tracksReleaseBranches,
            bool isCurrentBranchRelease,
            string commitDateFormat,
            int preReleaseWeight)
        {
            AssemblyVersioningScheme = assemblyVersioningScheme;
            AssemblyFileVersioningScheme = assemblyFileVersioningScheme;
            AssemblyInformationalFormat = assemblyInformationalFormat;
            AssemblyVersioningFormat = assemblyVersioningFormat;
            AssemblyFileVersioningFormat = assemblyFileVersioningFormat;
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
            NoBumpMessage = noBumpMessage;
            CommitMessageIncrementing = commitMessageIncrementing;
            LegacySemVerPadding = legacySemVerPaddding;
            BuildMetaDataPadding = buildMetaDataPadding;
            CommitsSinceVersionSourcePadding = commitsSinceVersionSourcePadding;
            VersionFilters = versionFilters;
            TracksReleaseBranches = tracksReleaseBranches;
            IsCurrentBranchRelease = isCurrentBranchRelease;
            CommitDateFormat = commitDateFormat;
            PreReleaseWeight = preReleaseWeight;
        }

        public bool TracksReleaseBranches { get; private set; }
        public bool IsCurrentBranchRelease { get; private set; }

        public VersioningMode VersioningMode { get; private set; }

        public AssemblyVersioningScheme AssemblyVersioningScheme { get; private set; }
        public AssemblyFileVersioningScheme AssemblyFileVersioningScheme { get; private set; }
        public string AssemblyInformationalFormat { get; private set; }
        public string AssemblyVersioningFormat { get; private set; }
        public string AssemblyFileVersioningFormat { get; private set; }

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

        public string NoBumpMessage { get; private set; }
        public int LegacySemVerPadding { get; private set; }
        public int BuildMetaDataPadding { get; private set; }

        public int CommitsSinceVersionSourcePadding { get; private set; }

        public CommitMessageIncrementMode CommitMessageIncrementing { get; private set; }

        public IEnumerable<IVersionFilter> VersionFilters { get; private set; }

        public string CommitDateFormat { get; private set; }

        public int PreReleaseWeight { get; private set; }
    }
}
