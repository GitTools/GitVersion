using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

/// <summary>
///     Configuration can be applied to different things, effective configuration is the result after applying the
///     appropriate configuration
/// </summary>
public record EffectiveConfiguration
{
    public EffectiveConfiguration(IGitVersionConfiguration configuration, IBranchConfiguration branchConfiguration)
    {
        configuration.NotNull();
        branchConfiguration.NotNull();

        var fallbackBranchConfiguration = configuration.GetFallbackBranchConfiguration();
        branchConfiguration = branchConfiguration.Inherit(fallbackBranchConfiguration);

        if (!branchConfiguration.DeploymentMode.HasValue)
            throw new("Configuration value for 'Versioning mode' has no value. (this should not happen, please report an issue)");

        if (!configuration.AssemblyVersioningScheme.HasValue)
            throw new("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");

        if (!configuration.AssemblyFileVersioningScheme.HasValue)
            throw new("Configuration value for 'AssemblyFileVersioningScheme' has no value. (this should not happen, please report an issue)");

        if (!branchConfiguration.CommitMessageIncrementing.HasValue)
            throw new("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");

        if (!configuration.TagPreReleaseWeight.HasValue)
            throw new("Configuration value for 'TagPreReleaseWeight' has no value. (this should not happen, please report an issue)");

        AssemblyVersioningScheme = configuration.AssemblyVersioningScheme.Value;
        AssemblyFileVersioningScheme = configuration.AssemblyFileVersioningScheme.Value;
        AssemblyInformationalFormat = configuration.AssemblyInformationalFormat;
        AssemblyVersioningFormat = configuration.AssemblyVersioningFormat;
        AssemblyFileVersioningFormat = configuration.AssemblyFileVersioningFormat;
        DeploymentMode = branchConfiguration.DeploymentMode.Value;
        TagPrefix = configuration.TagPrefix;
        VersionInBranchRegex = configuration.VersionInBranchRegex;
        Label = branchConfiguration.Label;
        NextVersion = configuration.NextVersion;
        Increment = branchConfiguration.Increment;
        RegularExpression = branchConfiguration.RegularExpression;
        PreventIncrementOfMergedBranchVersion = branchConfiguration.PreventIncrementOfMergedBranchVersion ?? false;
        LabelNumberPattern = branchConfiguration.LabelNumberPattern;
        TrackMergeTarget = branchConfiguration.TrackMergeTarget ?? false;
        TrackMergeMessage = branchConfiguration.TrackMergeMessage ?? true;
        MajorVersionBumpMessage = configuration.MajorVersionBumpMessage;
        MinorVersionBumpMessage = configuration.MinorVersionBumpMessage;
        PatchVersionBumpMessage = configuration.PatchVersionBumpMessage;
        NoBumpMessage = configuration.NoBumpMessage;
        CommitMessageIncrementing = branchConfiguration.CommitMessageIncrementing.Value;
        VersionFilters = configuration.Ignore.ToFilters();
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

    protected EffectiveConfiguration(AssemblyVersioningScheme assemblyVersioningScheme,
        AssemblyFileVersioningScheme assemblyFileVersioningScheme,
        string? assemblyInformationalFormat,
        string? assemblyVersioningFormat,
        string? assemblyFileVersioningFormat,
        DeploymentMode versioningMode,
        string? tagPrefix,
        string label,
        string? nextVersion,
        IncrementStrategy increment,
        string? regularExpression,
        bool preventIncrementOfMergedBranchVersion,
        string? labelNumberPattern,
        bool trackMergeTarget,
        string? majorVersionBumpMessage,
        string? minorVersionBumpMessage,
        string? patchVersionBumpMessage,
        string? noBumpMessage,
        CommitMessageIncrementMode commitMessageIncrementing,
        IEnumerable<IVersionFilter> versionFilters,
        bool tracksReleaseBranches,
        bool isReleaseBranch,
        bool isMainBranch,
        string? commitDateFormat,
        bool updateBuildNumber,
        SemanticVersionFormat semanticVersionFormat,
        int preReleaseWeight,
        int tagPreReleaseWeight)
    {
        AssemblyVersioningScheme = assemblyVersioningScheme;
        AssemblyFileVersioningScheme = assemblyFileVersioningScheme;
        AssemblyInformationalFormat = assemblyInformationalFormat;
        AssemblyVersioningFormat = assemblyVersioningFormat;
        AssemblyFileVersioningFormat = assemblyFileVersioningFormat;
        DeploymentMode = versioningMode;
        TagPrefix = tagPrefix;
        Label = label;
        NextVersion = nextVersion;
        Increment = increment;
        RegularExpression = regularExpression;
        PreventIncrementOfMergedBranchVersion = preventIncrementOfMergedBranchVersion;
        LabelNumberPattern = labelNumberPattern;
        TrackMergeTarget = trackMergeTarget;
        MajorVersionBumpMessage = majorVersionBumpMessage;
        MinorVersionBumpMessage = minorVersionBumpMessage;
        PatchVersionBumpMessage = patchVersionBumpMessage;
        NoBumpMessage = noBumpMessage;
        CommitMessageIncrementing = commitMessageIncrementing;
        VersionFilters = versionFilters;
        TracksReleaseBranches = tracksReleaseBranches;
        IsReleaseBranch = isReleaseBranch;
        IsMainBranch = isMainBranch;
        CommitDateFormat = commitDateFormat;
        UpdateBuildNumber = updateBuildNumber;
        SemanticVersionFormat = semanticVersionFormat;
        PreReleaseWeight = preReleaseWeight;
        TagPreReleaseWeight = tagPreReleaseWeight;
    }

    public bool TracksReleaseBranches { get; }
    public bool IsReleaseBranch { get; }
    public bool IsMainBranch { get; }
    public DeploymentMode DeploymentMode { get; }
    public AssemblyVersioningScheme AssemblyVersioningScheme { get; }
    public AssemblyFileVersioningScheme AssemblyFileVersioningScheme { get; }
    public string? AssemblyInformationalFormat { get; }
    public string? AssemblyVersioningFormat { get; }
    public string? AssemblyFileVersioningFormat { get; }

    /// <summary>
    ///     Git tag prefix
    /// </summary>
    public string? TagPrefix { get; }

    public Regex VersionInBranchRegex { get; }

    /// <summary>
    ///     Label to use when calculating SemVer
    /// </summary>
    public string? Label { get; }

    public string? NextVersion { get; }

    public IncrementStrategy Increment { get; }

    public string? RegularExpression { get; }

    public bool PreventIncrementOfMergedBranchVersion { get; }

    public string? LabelNumberPattern { get; }

    public bool TrackMergeTarget { get; }

    public bool TrackMergeMessage { get; }

    public string? MajorVersionBumpMessage { get; }

    public string? MinorVersionBumpMessage { get; }

    public string? PatchVersionBumpMessage { get; }

    public string? NoBumpMessage { get; }

    public CommitMessageIncrementMode CommitMessageIncrementing { get; }

    public IEnumerable<IVersionFilter> VersionFilters { get; }

    public string? CommitDateFormat { get; }

    public bool UpdateBuildNumber { get; }

    public SemanticVersionFormat SemanticVersionFormat { get; }

    public VersionStrategies VersionStrategy { get; }

    public int PreReleaseWeight { get; }

    public int TagPreReleaseWeight { get; }
}
