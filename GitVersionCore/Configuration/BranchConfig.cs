﻿namespace GitVersion
{
    using YamlDotNet.Serialization;

    public class BranchConfig
    {
        public BranchConfig()
        {
        }

        public BranchConfig(BranchConfig branchConfiguration)
        {
            VersioningMode = branchConfiguration.VersioningMode;
            Tag = branchConfiguration.Tag;
            Increment = branchConfiguration.Increment;
            PreventIncrementOfMergedBranchVersion = branchConfiguration.PreventIncrementOfMergedBranchVersion;
            TagNumberPattern = branchConfiguration.TagNumberPattern;
            TrackMergeTarget = branchConfiguration.TrackMergeTarget;
        }

        [YamlMember(Alias = "mode")]
        public VersioningMode? VersioningMode { get; set; }

        /// <summary>
        /// Special value 'useBranchName' will extract the tag from the branch name
        /// </summary>
        [YamlMember(Alias = "tag")]
        public string Tag { get; set; }

        [YamlMember(Alias = "increment")]
        public IncrementStrategy? Increment { get; set; }

        [YamlMember(Alias = "prevent-increment-of-merged-branch-version")]
        public bool? PreventIncrementOfMergedBranchVersion { get; set; }

        [YamlMember(Alias = "tag-number-pattern")]
        public string TagNumberPattern { get; set; }

        [YamlMember(Alias = "track-merge-target")]
        public bool TrackMergeTarget { get; set; }
    }
}
