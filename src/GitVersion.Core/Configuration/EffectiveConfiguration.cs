using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

/// <summary>
///     Configuration can be applied to different things, effective configuration is the result after applying the
///     appropriate configuration
/// </summary>
public record EffectiveConfiguration
{
    /// <summary>Initializes a new <see cref="EffectiveConfiguration"/> by merging global and branch-level configuration values.</summary>
    public EffectiveConfiguration(
        IGitVersionConfiguration configuration,
        IBranchConfiguration branchConfiguration,
        EffectiveConfiguration? fallbackConfiguration = null)
    {
        configuration.NotNull();
        branchConfiguration.NotNull();

        if (fallbackConfiguration is null)
        {
            var fallbackBranchConfiguration = configuration.GetFallbackBranchConfiguration();
            branchConfiguration = branchConfiguration.Inherit(fallbackBranchConfiguration);
        }
        else
        {
            branchConfiguration = branchConfiguration.Inherit(fallbackConfiguration);
        }

        if (!branchConfiguration.DeploymentMode.HasValue)
        {
            throw new InvalidOperationException("Configuration value for 'Deployment mode' has no value. (this should not happen, please report an issue)");
        }

        if (!configuration.AssemblyVersioningScheme.HasValue)
        {
            throw new InvalidOperationException("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");
        }

        if (!configuration.AssemblyFileVersioningScheme.HasValue)
        {
            throw new InvalidOperationException("Configuration value for 'AssemblyFileVersioningScheme' has no value. (this should not happen, please report an issue)");
        }

        if (!branchConfiguration.CommitMessageIncrementing.HasValue)
        {
            throw new InvalidOperationException("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");
        }

        if (!configuration.TagPreReleaseWeight.HasValue)
        {
            throw new InvalidOperationException("Configuration value for 'TagPreReleaseWeight' has no value. (this should not happen, please report an issue)");
        }

        if (configuration.CommitDateFormat.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Configuration value for 'CommitDateFormat' has no value. (this should not happen, please report an issue)");
        }

        AssemblyVersioningScheme = configuration.AssemblyVersioningScheme.Value;
        AssemblyFileVersioningScheme = configuration.AssemblyFileVersioningScheme.Value;
        AssemblyInformationalFormat = configuration.AssemblyInformationalFormat;
        AssemblyVersioningFormat = configuration.AssemblyVersioningFormat;
        AssemblyFileVersioningFormat = configuration.AssemblyFileVersioningFormat;
        CustomVersionFormat = configuration.CustomVersionFormat;
        DeploymentMode = branchConfiguration.DeploymentMode.Value;
        TagPrefixPattern = configuration.TagPrefixPattern;
        VersionInBranchPattern = configuration.VersionInBranchPattern;
        Label = branchConfiguration.Label;
        NextVersion = configuration.NextVersion;
        Increment = branchConfiguration.Increment;
        RegularExpression = branchConfiguration.RegularExpression;
        PreventIncrementOfMergedBranch = branchConfiguration.PreventIncrement.OfMergedBranch ?? false;
        PreventIncrementWhenBranchMerged = branchConfiguration.PreventIncrement.WhenBranchMerged ?? false;
        PreventIncrementWhenCurrentCommitTagged = branchConfiguration.PreventIncrement.WhenCurrentCommitTagged ?? true;
        TrackMergeTarget = branchConfiguration.TrackMergeTarget ?? false;
        TrackMergeMessage = branchConfiguration.TrackMergeMessage ?? true;
        MajorVersionBumpMessage = configuration.MajorVersionBumpMessage;
        MinorVersionBumpMessage = configuration.MinorVersionBumpMessage;
        PatchVersionBumpMessage = configuration.PatchVersionBumpMessage;
        NoBumpMessage = configuration.NoBumpMessage;
        CommitMessageIncrementing = branchConfiguration.CommitMessageIncrementing.Value;
        Ignore = configuration.Ignore;
        TracksReleaseBranches = branchConfiguration.TracksReleaseBranches ?? false;
        IsReleaseBranch = branchConfiguration.IsReleaseBranch ?? false;
        IsMainBranch = branchConfiguration.IsMainBranch ?? false;
        CommitDateFormat = configuration.CommitDateFormat;
        UpdateBuildNumber = configuration.UpdateBuildNumber;
        SemanticVersionFormat = configuration.SemanticVersionFormat;
        VersionStrategy = configuration.VersionStrategy;
        PreReleaseWeight = branchConfiguration.PreReleaseWeight ?? 0;
        TagPreReleaseWeight = configuration.TagPreReleaseWeight.Value;
    }

    /// <summary>Gets a value indicating whether this branch tracks release branches.</summary>
    public bool TracksReleaseBranches { get; }

    /// <summary>Gets a value indicating whether this branch is a release branch.</summary>
    public bool IsReleaseBranch { get; }

    /// <summary>Gets a value indicating whether this branch is a main/trunk branch.</summary>
    public bool IsMainBranch { get; }

    /// <summary>Gets the deployment mode used to calculate the next version.</summary>
    public DeploymentMode DeploymentMode { get; }

    /// <summary>Gets the scheme used to compute the <c>AssemblyVersionAttribute</c> value.</summary>
    public AssemblyVersioningScheme AssemblyVersioningScheme { get; }

    /// <summary>Gets the scheme used to compute the <c>AssemblyFileVersionAttribute</c> value.</summary>
    public AssemblyFileVersioningScheme AssemblyFileVersioningScheme { get; }

    /// <summary>Gets the format string used to compute the <c>AssemblyInformationalVersionAttribute</c> value.</summary>
    public string? AssemblyInformationalFormat { get; }

    /// <summary>Gets the format string used to compute the assembly version.</summary>
    public string? AssemblyVersioningFormat { get; }

    /// <summary>Gets the format string used to compute the assembly file version.</summary>
    public string? AssemblyFileVersioningFormat { get; }

    /// <summary>Gets the format string used to compute the custom version output.</summary>
    public string? CustomVersionFormat { get; }

    /// <summary>Gets the regex pattern that identifies tag prefixes to strip when parsing version tags.</summary>
    public string? TagPrefixPattern { get; }

    /// <summary>Gets the regex pattern used to extract a semantic version from a branch name.</summary>
    public string? VersionInBranchPattern { get; }

    /// <summary>Gets the pre-release label applied to versions on this branch.</summary>
    public string? Label { get; }

    /// <summary>Gets the manually configured next version, overriding automatic calculation.</summary>
    public string? NextVersion { get; }

    /// <summary>Gets the version field incremented when creating a new release from this branch.</summary>
    public IncrementStrategy Increment { get; }

    /// <summary>Gets the regular expression that matches branch names eligible for this configuration.</summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string? RegularExpression { get; }

    /// <summary>Gets a value indicating whether incrementing the version of a merged branch is prevented.</summary>
    public bool PreventIncrementOfMergedBranch { get; }

    /// <summary>Gets a value indicating whether incrementing when the branch itself is merged is prevented.</summary>
    public bool PreventIncrementWhenBranchMerged { get; }

    /// <summary>Gets a value indicating whether incrementing is prevented when the current commit is already tagged.</summary>
    public bool PreventIncrementWhenCurrentCommitTagged { get; }

    /// <summary>Gets a value indicating whether to track the merge target branch for version calculation.</summary>
    public bool TrackMergeTarget { get; }

    /// <summary>Gets a value indicating whether merge commit messages are used to determine version increments.</summary>
    public bool TrackMergeMessage { get; }

    /// <summary>Gets the commit message pattern that triggers a major version bump.</summary>
    public string? MajorVersionBumpMessage { get; }

    /// <summary>Gets the commit message pattern that triggers a minor version bump.</summary>
    public string? MinorVersionBumpMessage { get; }

    /// <summary>Gets the commit message pattern that triggers a patch version bump.</summary>
    public string? PatchVersionBumpMessage { get; }

    /// <summary>Gets the commit message pattern that suppresses any version bump.</summary>
    public string? NoBumpMessage { get; }

    /// <summary>Gets the mode that controls how commit messages are used to determine version increments.</summary>
    public CommitMessageIncrementMode CommitMessageIncrementing { get; }

    /// <summary>Gets the configuration that controls which commits and time ranges are ignored during version calculation.</summary>
    public IIgnoreConfiguration Ignore { get; }

    /// <summary>Gets the format string used when rendering the commit date in version output.</summary>
    public string? CommitDateFormat { get; }

    /// <summary>Gets a value indicating whether the build number in the CI system should be updated.</summary>
    public bool UpdateBuildNumber { get; }

    /// <summary>Gets the semantic version format (strict or loose) used when parsing version strings.</summary>
    public SemanticVersionFormat SemanticVersionFormat { get; }

    /// <summary>Gets the set of version strategies enabled for this configuration.</summary>
    public VersionStrategies VersionStrategy { get; }

    /// <summary>Gets the numeric weight applied to the pre-release tag number to produce a weighted pre-release number.</summary>
    public int PreReleaseWeight { get; }

    /// <summary>Gets the pre-release weight applied to tagged commits when calculating the weighted pre-release number.</summary>
    public int TagPreReleaseWeight { get; }
}
