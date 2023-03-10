using System.Text.RegularExpressions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public interface IBranchConfiguration
{
    VersioningMode? VersioningMode { get; }

    string? Label { get; }

    IncrementStrategy Increment { get; }

    bool? PreventIncrementOfMergedBranchVersion { get; }

    string? LabelNumberPattern { get; }

    bool? TrackMergeTarget { get; }

    bool? TrackMergeMessage { get; }

    CommitMessageIncrementMode? CommitMessageIncrementing { get; }

    public string? RegularExpression { get; }

    public bool IsMatch(string branchName)
        => RegularExpression != null && Regex.IsMatch(branchName, RegularExpression, RegexOptions.IgnoreCase);

    bool? TracksReleaseBranches { get; }

    bool? IsReleaseBranch { get; }

    bool? IsMainline { get; }

    int? PreReleaseWeight { get; }

    IReadOnlyCollection<string> SourceBranches { get; }

    IReadOnlyCollection<string> IsSourceBranchFor { get; }

    BranchConfiguration Inherit(BranchConfiguration configuration);
}
