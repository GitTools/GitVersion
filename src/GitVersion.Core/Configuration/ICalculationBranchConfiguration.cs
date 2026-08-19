using System.Diagnostics.CodeAnalysis;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

/// <summary>Represents branch settings that affect semantic-version calculation.</summary>
public interface ICalculationBranchConfiguration
{
    /// <summary>Gets the deployment mode used to compute versions on this branch.</summary>
    DeploymentMode? DeploymentMode { get; }

    /// <summary>Gets the pre-release label applied to versions produced on this branch.</summary>
    string? Label { get; }

    /// <summary>Gets the version field that is incremented when creating a new release from this branch.</summary>
    IncrementStrategy Increment { get; }

    /// <summary>Gets the configuration that controls under what conditions automatic version increments are suppressed.</summary>
    IPreventIncrementConfiguration PreventIncrement { get; }

    /// <summary>Gets a value indicating whether to track the merge target branch for version calculation.</summary>
    bool? TrackMergeTarget { get; }

    /// <summary>Gets a value indicating whether merge commit messages are considered when determining version increments.</summary>
    bool? TrackMergeMessage { get; }

    /// <summary>Gets the mode that controls how commit messages drive version increments.</summary>
    CommitMessageIncrementMode? CommitMessageIncrementing { get; }

    /// <summary>Gets the regular expression that matches branch names eligible for this configuration.</summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    string? RegularExpression { get; }

    /// <summary>Gets the names of branches that this branch may be branched from.</summary>
    IReadOnlyCollection<string> SourceBranches { get; }

    /// <summary>Gets the names of branches for which this branch may act as a source.</summary>
    IReadOnlyCollection<string> IsSourceBranchFor { get; }

    /// <summary>Gets a value indicating whether this branch tracks release branches.</summary>
    bool? TracksReleaseBranches { get; }

    /// <summary>Gets a value indicating whether this branch is a release branch.</summary>
    bool? IsReleaseBranch { get; }

    /// <summary>Gets a value indicating whether this branch is treated as a main/trunk branch.</summary>
    bool? IsMainBranch { get; }
}
