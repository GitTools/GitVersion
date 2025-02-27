using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public interface IBranchConfiguration
{
    DeploymentMode? DeploymentMode { get; }

    string? Label { get; }

    IncrementStrategy Increment { get; }

    IPreventIncrementConfiguration PreventIncrement { get; }

    bool? TrackMergeTarget { get; }

    bool? TrackMergeMessage { get; }

    CommitMessageIncrementMode? CommitMessageIncrementing { get; }

    string? RegularExpression { get; }

    bool IsMatch(string branchName)
    {
        if (string.IsNullOrWhiteSpace(RegularExpression))
        {
            return false;
        }

        var regex = RegexPatterns.Cache.GetOrAdd(RegularExpression);
        return regex.IsMatch(branchName);
    }

    IReadOnlyCollection<string> SourceBranches { get; }

    IReadOnlyCollection<string> IsSourceBranchFor { get; }

    bool? TracksReleaseBranches { get; }

    bool? IsReleaseBranch { get; }

    bool? IsMainBranch { get; }

    int? PreReleaseWeight { get; }

    IBranchConfiguration Inherit(IBranchConfiguration configuration);

    IBranchConfiguration Inherit(EffectiveConfiguration configuration);
}
