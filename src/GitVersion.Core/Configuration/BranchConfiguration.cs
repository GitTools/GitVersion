using GitVersion.VersionCalculation;
using YamlDotNet.Serialization;

namespace GitVersion.Configuration;

public class BranchConfiguration
{
    public BranchConfiguration()
    {
    }

    /// <summary>
    /// Creates a clone of the given <paramref name="branchConfiguration"/>.
    /// </summary>
    public BranchConfiguration(BranchConfiguration branchConfiguration)
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

    public BranchConfiguration Inherit(BranchConfiguration? parentConfig)
    {
        if (parentConfig is null) return this;

        var result = new BranchConfiguration(this);

        if (result.Increment is null || result.Increment == IncrementStrategy.Inherit)
            result.Increment = parentConfig.Increment;
        result.VersioningMode ??= parentConfig.VersioningMode;
        result.Tag ??= parentConfig.Tag;
        result.PreventIncrementOfMergedBranchVersion ??= parentConfig.PreventIncrementOfMergedBranchVersion;
        result.TagNumberPattern ??= parentConfig.TagNumberPattern;
        result.TrackMergeTarget ??= parentConfig.TrackMergeTarget;
        result.CommitMessageIncrementing ??= parentConfig.CommitMessageIncrementing;
        result.Regex ??= parentConfig.Regex;
        result.SourceBranches ??= parentConfig.SourceBranches;
        result.IsSourceBranchFor ??= parentConfig.IsSourceBranchFor;
        result.TracksReleaseBranches ??= parentConfig.TracksReleaseBranches;
        result.IsReleaseBranch ??= parentConfig.IsReleaseBranch;
        result.IsMainline ??= parentConfig.IsMainline;
        result.PreReleaseWeight ??= parentConfig.PreReleaseWeight;

        return result;
    }

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
    /// The name given to this configuration in the configuration file.
    /// </summary>
    [YamlIgnore]
    public string Name { get; set; }

    public void MergeTo(BranchConfiguration targetConfig)
    {
        if (targetConfig == null) throw new ArgumentNullException(nameof(targetConfig));

        targetConfig.VersioningMode = VersioningMode ?? targetConfig.VersioningMode;
        targetConfig.Tag = Tag ?? targetConfig.Tag;
        targetConfig.Increment = Increment ?? targetConfig.Increment;
        targetConfig.PreventIncrementOfMergedBranchVersion = PreventIncrementOfMergedBranchVersion ?? targetConfig.PreventIncrementOfMergedBranchVersion;
        targetConfig.TagNumberPattern = TagNumberPattern ?? targetConfig.TagNumberPattern;
        targetConfig.TrackMergeTarget = TrackMergeTarget ?? targetConfig.TrackMergeTarget;
        targetConfig.CommitMessageIncrementing = CommitMessageIncrementing ?? targetConfig.CommitMessageIncrementing;
        targetConfig.Regex = Regex ?? targetConfig.Regex;
        targetConfig.SourceBranches = SourceBranches ?? targetConfig.SourceBranches;
        targetConfig.IsSourceBranchFor = IsSourceBranchFor ?? targetConfig.IsSourceBranchFor;
        targetConfig.TracksReleaseBranches = TracksReleaseBranches ?? targetConfig.TracksReleaseBranches;
        targetConfig.IsReleaseBranch = IsReleaseBranch ?? targetConfig.IsReleaseBranch;
        targetConfig.IsMainline = IsMainline ?? targetConfig.IsMainline;
        targetConfig.PreReleaseWeight = PreReleaseWeight ?? targetConfig.PreReleaseWeight;
    }

    public BranchConfiguration Apply(BranchConfiguration overrides)
    {
        if (overrides == null) throw new ArgumentNullException(nameof(overrides));

        overrides.MergeTo(this);
        return this;
    }
}
