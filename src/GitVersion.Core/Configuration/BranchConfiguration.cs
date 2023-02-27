using GitVersion.Attributes;
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
    [JsonPropertyDescription("The versioning mode for this branch. Can be 'ContinuousDelivery', 'ContinuousDeployment', 'Mainline'.")]
    public VersioningMode? VersioningMode { get; set; }

    /// <summary>
    /// Special value 'useBranchName' will extract the tag from the branch name
    /// </summary>
    [JsonPropertyName("label")]
    [JsonPropertyDescription("The label to use for this branch. Can be 'useBranchName' to extract the label from the branch name. Use the value {BranchName} as a placeholder to insert the branch name.")]
    public string? Label { get; set; }

    [JsonPropertyName("increment")]
    [JsonPropertyDescription("The increment strategy for this branch. Can be 'Inherit', 'Patch', 'Minor', 'Major', 'None'.")]
    public IncrementStrategy Increment { get; set; }

    [JsonPropertyName("prevent-increment-of-merged-branch-version")]
    [JsonPropertyDescription("Prevent increment of merged branch version.")]
    public bool? PreventIncrementOfMergedBranchVersion { get; set; }

    [JsonPropertyName("label-number-pattern")]
    [JsonPropertyDescription(@"The regex pattern to use to extract the number from the branch name. Defaults to '[/-](?<number>\d+)[-/]'.")]
    [JsonPropertyPattern(@"[/-](?<number>\d+)[-/]")]
    public string? LabelNumberPattern { get; set; }

    [JsonPropertyName("track-merge-target")]
    [JsonPropertyDescription("Strategy which will look for tagged merge commits directly off the current branch.")]
    public bool? TrackMergeTarget { get; set; }

    [JsonPropertyName("track-merge-message")]
    [JsonPropertyDescription("This property is a branch related property and gives the user the possibility to control the behavior of whether the merge commit message will be interpreted as a next version or not.")]
    public bool? TrackMergeMessage { get; set; }

    [JsonPropertyName("commit-message-incrementing")]
    [JsonPropertyDescription("Sets whether it should be possible to increment the version with special syntax in the commit message. Can be 'Disabled', 'Enabled' or 'MergeMessageOnly'.")]
    public CommitMessageIncrementMode? CommitMessageIncrementing { get; set; }

    [JsonPropertyName("regex")]
    [JsonPropertyDescription("The regex pattern to use to match this branch.")]
    public string? Regex { get; set; }

    [JsonPropertyName("source-branches")]
    [JsonPropertyDescription("The source branches for this branch.")]
    public HashSet<string>? SourceBranches { get; set; }

    [JsonPropertyName("is-source-branch-for")]
    [JsonPropertyDescription("The branches that this branch is a source branch.")]
    public HashSet<string>? IsSourceBranchFor { get; set; }

    [JsonPropertyName("tracks-release-branches")]
    [JsonPropertyDescription("Indicates this branch config represents develop in GitFlow.")]
    public bool? TracksReleaseBranches { get; set; }

    [JsonPropertyName("is-release-branch")]
    [JsonPropertyDescription("Indicates this branch config represents a release branch in GitFlow.")]
    public bool? IsReleaseBranch { get; set; }

    [JsonPropertyName("is-mainline")]
    [JsonPropertyDescription("When using Mainline mode, this indicates that this branch is a mainline. By default main and support/* are mainlines.")]
    public bool? IsMainline { get; set; }

    [JsonPropertyName("pre-release-weight")]
    [JsonPropertyDescription("Provides a way to translate the PreReleaseLabel to a number.")]
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

        if (result.Increment == IncrementStrategy.Inherit)
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
