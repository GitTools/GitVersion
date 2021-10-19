using GitVersion.VersionCalculation;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace GitVersion.Model.Configuration;

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
    public string? Tag { get; set; }

    [YamlMember(Alias = "increment")]
    public IncrementStrategy? Increment { get; set; }

    [YamlMember(Alias = "prevent-increment-of-merged-branch-version")]
    public bool? PreventIncrementOfMergedBranchVersion { get; set; }

    [YamlMember(Alias = "tag-number-pattern")]
    public string? TagNumberPattern { get; set; }

    [YamlMember(Alias = "track-merge-target")]
    public bool? TrackMergeTarget { get; set; }

    [YamlMember(Alias = "commit-message-incrementing")]
    public CommitMessageIncrementMode? CommitMessageIncrementing { get; set; }

    [YamlMember(Alias = "regex")]
    public string? Regex { get; set; }

    [YamlMember(Alias = "source-branches")]
    public HashSet<string>? SourceBranches { get; set; }

    [YamlMember(Alias = "is-source-branch-for")]
    public HashSet<string>? IsSourceBranchFor { get; set; }

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
    public string? Name { get; set; }

    public void MergeTo([NotNull] BranchConfig targetConfig)
    {
        if (targetConfig == null) throw new ArgumentNullException(nameof(targetConfig));

        targetConfig.VersioningMode = this.VersioningMode ?? targetConfig.VersioningMode;
        targetConfig.Tag = this.Tag ?? targetConfig.Tag;
        targetConfig.Increment = this.Increment ?? targetConfig.Increment;
        targetConfig.PreventIncrementOfMergedBranchVersion = this.PreventIncrementOfMergedBranchVersion ?? targetConfig.PreventIncrementOfMergedBranchVersion;
        targetConfig.TagNumberPattern = this.TagNumberPattern ?? targetConfig.TagNumberPattern;
        targetConfig.TrackMergeTarget = this.TrackMergeTarget ?? targetConfig.TrackMergeTarget;
        targetConfig.CommitMessageIncrementing = this.CommitMessageIncrementing ?? targetConfig.CommitMessageIncrementing;
        targetConfig.Regex = this.Regex ?? targetConfig.Regex;
        targetConfig.SourceBranches = this.SourceBranches ?? targetConfig.SourceBranches;
        targetConfig.IsSourceBranchFor = this.IsSourceBranchFor ?? targetConfig.IsSourceBranchFor;
        targetConfig.TracksReleaseBranches = this.TracksReleaseBranches ?? targetConfig.TracksReleaseBranches;
        targetConfig.IsReleaseBranch = this.IsReleaseBranch ?? targetConfig.IsReleaseBranch;
        targetConfig.IsMainline = this.IsMainline ?? targetConfig.IsMainline;
        targetConfig.PreReleaseWeight = this.PreReleaseWeight ?? targetConfig.PreReleaseWeight;
    }

    public BranchConfig Apply([NotNull] BranchConfig overrides)
    {
        if (overrides == null) throw new ArgumentNullException(nameof(overrides));

        overrides.MergeTo(this);
        return this;
    }

    public static BranchConfig CreateDefaultBranchConfig(string name) => new()
    {
        Name = name,
        Tag = "useBranchName",
        PreventIncrementOfMergedBranchVersion = false,
        TrackMergeTarget = false,
        TracksReleaseBranches = false,
        IsReleaseBranch = false,
        IsMainline = false,
    };
}
