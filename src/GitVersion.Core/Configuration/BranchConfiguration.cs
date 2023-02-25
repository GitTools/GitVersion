using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public class BranchConfiguration
{
    public BranchConfiguration()
    {
    }

    public BranchConfiguration(BranchConfiguration branchConfiguration)
    {
        VersioningMode = branchConfiguration.VersioningMode;
        Label = branchConfiguration.Label;
        Increment = branchConfiguration.Increment;
        PreventIncrementOfMergedBranchVersion = branchConfiguration.PreventIncrementOfMergedBranchVersion;
        LabelNumberPattern = branchConfiguration.LabelNumberPattern;
        TrackMergeTarget = branchConfiguration.TrackMergeTarget;
        TrackMergeMessage = branchConfiguration.TrackMergeMessage;
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

    [JsonPropertyName("mode")]
    public VersioningMode? VersioningMode { get; set; }

    /// <summary>
    /// Special value 'useBranchName' will extract the tag from the branch name
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("increment")]
    public IncrementStrategy? Increment { get; set; }

    [JsonPropertyName("prevent-increment-of-merged-branch-version")]
    public bool? PreventIncrementOfMergedBranchVersion { get; set; }

    [JsonPropertyName("label-number-pattern")]
    public string? LabelNumberPattern { get; set; }

    [JsonPropertyName("track-merge-target")]
    public bool? TrackMergeTarget { get; set; }

    [JsonPropertyName("track-merge-message")]
    public bool? TrackMergeMessage { get; set; }

    [JsonPropertyName("commit-message-incrementing")]
    public CommitMessageIncrementMode? CommitMessageIncrementing { get; set; }

    [JsonPropertyName("regex")]
    public string? Regex { get; set; }

    [JsonPropertyName("source-branches")]
    public HashSet<string>? SourceBranches { get; set; }

    [JsonPropertyName("is-source-branch-for")]
    public HashSet<string>? IsSourceBranchFor { get; set; }

    [JsonPropertyName("tracks-release-branches")]
    public bool? TracksReleaseBranches { get; set; }

    [JsonPropertyName("is-release-branch")]
    public bool? IsReleaseBranch { get; set; }

    [JsonPropertyName("is-mainline")]
    public bool? IsMainline { get; set; }

    [JsonPropertyName("pre-release-weight")]
    public int? PreReleaseWeight { get; set; }

    /// <summary>
    /// The name given to this configuration in the configuration file.
    /// </summary>
    [JsonIgnore]
    public string Name { get; set; }

    public BranchConfiguration Inherit(BranchConfiguration? parentConfig)
    {
        if (parentConfig is null) return this;

        var result = new BranchConfiguration(this);

        if (result.Increment is null || result.Increment == IncrementStrategy.Inherit)
            result.Increment = parentConfig.Increment;
        result.VersioningMode ??= parentConfig.VersioningMode;
        result.Label ??= parentConfig.Label;
        result.PreventIncrementOfMergedBranchVersion ??= parentConfig.PreventIncrementOfMergedBranchVersion;
        result.LabelNumberPattern ??= parentConfig.LabelNumberPattern;
        result.TrackMergeTarget ??= parentConfig.TrackMergeTarget;
        result.TrackMergeMessage ??= parentConfig.TrackMergeMessage;
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
}
