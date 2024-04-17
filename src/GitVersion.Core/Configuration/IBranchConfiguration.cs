using System.Text.RegularExpressions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public interface IBranchConfiguration
{
    DeploymentMode? DeploymentMode { get; }

    string? Label { get; }

    IncrementStrategy Increment { get; }

    IPreventIncrementConfiguration PreventIncrement { get; }

    string? LabelNumberPattern { get; }

    bool? TrackMergeTarget { get; }

    bool? TrackMergeMessage { get; }

    CommitMessageIncrementMode? CommitMessageIncrementing { get; }

    public string? RegularExpression { get; }

    public bool IsMatch(string branchName)
        => RegularExpression != null && Regex.IsMatch(branchName, RegularExpression, RegexOptions.IgnoreCase);

    IReadOnlyCollection<string> SourceBranches { get; }

    IReadOnlyCollection<string> IsSourceBranchFor { get; }

    bool? TracksReleaseBranches { get; }

    bool? IsReleaseBranch { get; }

    bool? IsMainBranch { get; }

    int? PreReleaseWeight { get; }

    IBranchConfiguration Inherit(IBranchConfiguration configuration);

    IBranchConfiguration Inherit(EffectiveConfiguration configuration);
}
