using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal class BranchConfigurationBuilder
{
    public static BranchConfigurationBuilder New => new();

    private DeploymentMode? deploymentMode;
    private string? label;
    private IncrementStrategy increment;
    private bool? preventIncrementOfMergedBranch;
    private bool? preventIncrementWhenBranchMerged;
    private bool? preventIncrementWhenCurrentCommitTagged;
    private string? labelNumberPattern;
    private bool? trackMergeTarget;
    private bool? trackMergeMessage;
    private CommitMessageIncrementMode? commitMessageIncrementing;
    private string? regularExpression;
    private HashSet<string> sourceBranches = [];
    private HashSet<string> isSourceBranchFor = [];
    private bool? tracksReleaseBranches;
    private bool? isReleaseBranch;
    private bool? isMainBranch;
    private int? preReleaseWeight;

    private BranchConfigurationBuilder()
    {
    }

    public virtual BranchConfigurationBuilder WithDeploymentMode(DeploymentMode? value)
    {
        this.deploymentMode = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithLabel(string? value)
    {
        this.label = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithIncrement(IncrementStrategy value)
    {
        this.increment = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithPreventIncrementOfMergedBranch(bool? value)
    {
        this.preventIncrementOfMergedBranch = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithPreventIncrementWhenBranchMerged(bool? value)
    {
        this.preventIncrementWhenBranchMerged = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithPreventIncrementWhenCurrentCommitTagged(bool? value)
    {
        this.preventIncrementWhenCurrentCommitTagged = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithLabelNumberPattern(string? value)
    {
        this.labelNumberPattern = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithTrackMergeTarget(bool? value)
    {
        this.trackMergeTarget = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithTrackMergeMessage(bool? value)
    {
        this.trackMergeMessage = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithCommitMessageIncrementing(CommitMessageIncrementMode? value)
    {
        this.commitMessageIncrementing = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithRegularExpression(string? value)
    {
        this.regularExpression = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithSourceBranches(IEnumerable<string> values)
    {
        WithSourceBranches(values.ToArray());
        return this;
    }

    public virtual BranchConfigurationBuilder WithSourceBranches(params string[] values)
    {
        this.sourceBranches = [.. values];
        return this;
    }

    public virtual BranchConfigurationBuilder WithIsSourceBranchFor(IEnumerable<string> values)
    {
        WithIsSourceBranchFor(values.ToArray());
        return this;
    }

    public virtual BranchConfigurationBuilder WithIsSourceBranchFor(params string[] values)
    {
        this.isSourceBranchFor = [.. values];
        return this;
    }

    public virtual BranchConfigurationBuilder WithTracksReleaseBranches(bool? value)
    {
        this.tracksReleaseBranches = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithIsReleaseBranch(bool? value)
    {
        this.isReleaseBranch = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithIsMainBranch(bool? value)
    {
        this.isMainBranch = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithPreReleaseWeight(int? value)
    {
        this.preReleaseWeight = value;
        return this;
    }

    public virtual BranchConfigurationBuilder WithConfiguration(IBranchConfiguration value)
    {
        WithDeploymentMode(value.DeploymentMode);
        WithLabel(value.Label);
        WithIncrement(value.Increment);
        WithPreventIncrementOfMergedBranch(value.PreventIncrement.OfMergedBranch);
        WithPreventIncrementWhenBranchMerged(value.PreventIncrement.WhenBranchMerged);
        WithPreventIncrementWhenCurrentCommitTagged(value.PreventIncrement.WhenCurrentCommitTagged);
        WithLabelNumberPattern(value.LabelNumberPattern);
        WithTrackMergeTarget(value.TrackMergeTarget);
        WithTrackMergeMessage(value.TrackMergeMessage);
        WithCommitMessageIncrementing(value.CommitMessageIncrementing);
        WithRegularExpression(value.RegularExpression);
        WithTracksReleaseBranches(value.TracksReleaseBranches);
        WithIsReleaseBranch(value.IsReleaseBranch);
        WithIsMainBranch(value.IsMainBranch);
        WithPreReleaseWeight(value.PreReleaseWeight);
        WithSourceBranches(value.SourceBranches);
        WithIsSourceBranchFor(value.IsSourceBranchFor);
        return this;
    }

    public IBranchConfiguration Build() => new BranchConfiguration
    {
        DeploymentMode = deploymentMode,
        Label = label,
        Increment = increment,
        RegularExpression = regularExpression,
        TracksReleaseBranches = tracksReleaseBranches,
        TrackMergeTarget = trackMergeTarget,
        TrackMergeMessage = trackMergeMessage,
        CommitMessageIncrementing = commitMessageIncrementing,
        IsMainBranch = isMainBranch,
        IsReleaseBranch = isReleaseBranch,
        LabelNumberPattern = labelNumberPattern,
        PreventIncrement = new PreventIncrementConfiguration()
        {
            OfMergedBranch = preventIncrementOfMergedBranch,
            WhenBranchMerged = preventIncrementWhenBranchMerged,
            WhenCurrentCommitTagged = preventIncrementWhenCurrentCommitTagged
        },
        PreReleaseWeight = preReleaseWeight,
        SourceBranches = sourceBranches,
        IsSourceBranchFor = isSourceBranchFor
    };
}
