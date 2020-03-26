using System.Collections.Generic;
using GitVersion.VersionCalculation;
using YamlDotNet.Serialization;

namespace GitVersion.Model.Configuration
{
    public class BranchConfig
    {
        public BranchConfig()
        {
        }

        /// <summary>
        /// Creates a clone of the given <paramref name="branchConfiguration"/>.
        /// </summary>
        public BranchConfig(BranchConfig branchConfiguration)
        {
            VersioningMode = branchConfiguration.VersioningMode;
            Tag = branchConfiguration.Tag;
            Increment = branchConfiguration.Increment;
            PreventIncrementOfMergedBranchVersion = branchConfiguration.PreventIncrementOfMergedBranchVersion;
            TagNumberPattern = branchConfiguration.TagNumberPattern;
            TrackMergeTarget = branchConfiguration.TrackMergeTarget;
            CommitMessageIncrementing = branchConfiguration.CommitMessageIncrementing;
            TracksReleaseBranches = branchConfiguration.TracksReleaseBranches;
            Regex = branchConfiguration.Regex;
            IsReleaseBranch = branchConfiguration.IsReleaseBranch;
            IsMainline = branchConfiguration.IsMainline;
            Name = branchConfiguration.Name;
            SourceBranches = branchConfiguration.SourceBranches;
            IsSourceBranchFor = branchConfiguration.IsSourceBranchFor;
            PreReleaseWeight = branchConfiguration.PreReleaseWeight;
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
        public bool? TrackMergeTarget { get; set; }

        [YamlMember(Alias = "commit-message-incrementing")]
        public CommitMessageIncrementMode? CommitMessageIncrementing { get; set; }

        [YamlMember(Alias = "regex")]
        public string Regex { get; set; }

        [YamlMember(Alias = "source-branches")]
        public List<string> SourceBranches { get; set; }

        [YamlMember(Alias = "is-source-branch-for")]
        public string[] IsSourceBranchFor { get; set; }

        [YamlMember(Alias = "tracks-release-branches")]
        public bool? TracksReleaseBranches { get; set; }

        [YamlMember(Alias = "is-release-branch")]
        public bool? IsReleaseBranch { get; set; }

        [YamlMember(Alias = "is-mainline")]
        public bool? IsMainline { get; set; }

        [YamlMember(Alias = "pre-release-weight")]
        public int? PreReleaseWeight { get; set; }

        /// <summary>
        /// The name given to this configuration in the config file.
        /// </summary>
        [YamlIgnore]
        public string Name { get; set; }
    }
}
