using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

public class TestBranchConfigurationBuilder
{
    public static TestBranchConfigurationBuilder New => new();

    private string name;
    private VersioningMode? versioningMode;
    private string? tag;
    private IncrementStrategy? increment;
    private bool? preventIncrementOfMergedBranchVersion;
    private string? tagNumberPattern;
    private bool? trackMergeTarget;
    private CommitMessageIncrementMode? commitMessageIncrementing;
    private string? regex;
    private HashSet<string>? sourceBranches;
    private HashSet<string>? isSourceBranchFor;
    private bool? tracksReleaseBranches;
    private bool? isReleaseBranch;
    private bool? isMainline;
    private int? preReleaseWeight;

    private TestBranchConfigurationBuilder() => this.name = "Just-A-Test";

    public virtual TestBranchConfigurationBuilder WithName(string value)
    {
        this.name = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithVersioningMode(VersioningMode? value)
    {
        this.versioningMode = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithTag(string? value)
    {
        this.tag = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithIncrement(IncrementStrategy? value)
    {
        this.increment = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithPreventIncrementOfMergedBranchVersion(bool? value)
    {
        this.preventIncrementOfMergedBranchVersion = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithTagNumberPattern(string? value)
    {
        this.tagNumberPattern = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithTrackMergeTarget(bool? value)
    {
        this.trackMergeTarget = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithCommitMessageIncrementing(CommitMessageIncrementMode? value)
    {
        this.commitMessageIncrementing = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithRegex(string? value)
    {
        this.regex = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithSourceBranches(IEnumerable<string> values)
    {
        WithSourceBranches(values.ToArray());
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithSourceBranches(params string[] values)
    {
        this.sourceBranches = new HashSet<string>(values);
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithIsSourceBranchFor(IEnumerable<string> values)
    {
        WithIsSourceBranchFor(values.ToArray());
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithIsSourceBranchFor(params string[] values)
    {
        this.isSourceBranchFor = new HashSet<string>(values);
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithTracksReleaseBranches(bool? value)
    {
        this.tracksReleaseBranches = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithIsReleaseBranch(bool? value)
    {
        this.isReleaseBranch = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithIsMainline(bool? value)
    {
        this.isMainline = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithPreReleaseWeight(int? value)
    {
        this.preReleaseWeight = value;
        return this;
    }

    public virtual TestBranchConfigurationBuilder WithConfiguration(BranchConfiguration value)
    {
        WithName(value.Name);
        WithVersioningMode(value.VersioningMode);
        WithTag(value.Tag);
        WithIncrement(value.Increment);
        WithPreventIncrementOfMergedBranchVersion(value.PreventIncrementOfMergedBranchVersion);
        WithTagNumberPattern(value.TagNumberPattern);
        WithTrackMergeTarget(value.TrackMergeTarget);
        WithCommitMessageIncrementing(value.CommitMessageIncrementing);
        WithRegex(value.Regex);
        WithTracksReleaseBranches(value.TracksReleaseBranches);
        WithIsReleaseBranch(value.IsReleaseBranch);
        WithIsMainline(value.IsMainline);
        WithPreReleaseWeight(value.PreReleaseWeight);
        WithSourceBranches(value.SourceBranches ?? Enumerable.Empty<string>());
        WithIsSourceBranchFor(value.IsSourceBranchFor ?? Enumerable.Empty<string>());
        return this;
    }

    public BranchConfiguration Build()
    {
        var result = new BranchConfiguration()
        {
            Name = name,
            VersioningMode = versioningMode,
            Tag = tag,
            Increment = increment,
            Regex = regex,
            TracksReleaseBranches = tracksReleaseBranches,
            TrackMergeTarget = trackMergeTarget,
            CommitMessageIncrementing = commitMessageIncrementing,
            IsMainline = isMainline,
            IsReleaseBranch = isReleaseBranch,
            TagNumberPattern = tagNumberPattern,
            PreventIncrementOfMergedBranchVersion = preventIncrementOfMergedBranchVersion,
            PreReleaseWeight = preReleaseWeight,
            SourceBranches = sourceBranches,
            IsSourceBranchFor = isSourceBranchFor
        };
        return result;
    }
}
