using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Model.Configuration;

/// <summary>
///     Configuration can be applied to different things, effective configuration is the result after applying the
///     appropriate configuration
/// </summary>
public class EffectiveConfiguration
{
    public EffectiveConfiguration(Config configuration, BranchConfig currentBranchConfig)
    {
        configuration.NotNull();
        currentBranchConfig.NotNull();

        var name = currentBranchConfig.Name;

        if (!currentBranchConfig.VersioningMode.HasValue)
            throw new Exception($"Configuration value for 'Versioning mode' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!currentBranchConfig.Increment.HasValue)
            throw new Exception($"Configuration value for 'Increment' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!configuration.AssemblyVersioningScheme.HasValue)
            throw new Exception("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");

        if (!configuration.AssemblyFileVersioningScheme.HasValue)
            throw new Exception("Configuration value for 'AssemblyFileVersioningScheme' has no value. (this should not happen, please report an issue)");

        if (!configuration.CommitMessageIncrementing.HasValue)
            throw new Exception("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");

        if (!configuration.TagPreReleaseWeight.HasValue)
            throw new Exception("Configuration value for 'TagPreReleaseWeight' has no value. (this should not happen, please report an issue)");

        AssemblyVersioningScheme = configuration.AssemblyVersioningScheme.Value;
        AssemblyFileVersioningScheme = configuration.AssemblyFileVersioningScheme.Value;
        AssemblyInformationalFormat = configuration.AssemblyInformationalFormat;
        AssemblyVersioningFormat = configuration.AssemblyVersioningFormat;
        AssemblyFileVersioningFormat = configuration.AssemblyFileVersioningFormat;
        VersioningMode = currentBranchConfig.VersioningMode.Value;
        TagPrefix = configuration.TagPrefix;
        Tag = currentBranchConfig.Tag ?? @"{BranchName}";
        NextVersion = configuration.NextVersion;
        Increment = currentBranchConfig.Increment.Value;
        BranchPrefixToTrim = currentBranchConfig.Regex;
        PreventIncrementOfMergedBranchVersion = currentBranchConfig.PreventIncrementOfMergedBranchVersion ?? false;
        TagNumberPattern = currentBranchConfig.TagNumberPattern;
        ContinuousDeploymentFallbackTag = configuration.ContinuousDeploymentFallbackTag;
        TrackMergeTarget = currentBranchConfig.TrackMergeTarget ?? false;
        MajorVersionBumpMessage = configuration.MajorVersionBumpMessage;
        MinorVersionBumpMessage = configuration.MinorVersionBumpMessage;
        PatchVersionBumpMessage = configuration.PatchVersionBumpMessage;
        NoBumpMessage = configuration.NoBumpMessage;
        CommitMessageIncrementing = currentBranchConfig.CommitMessageIncrementing ?? configuration.CommitMessageIncrementing.Value;
        VersionFilters = configuration.Ignore.ToFilters();
        TracksReleaseBranches = currentBranchConfig.TracksReleaseBranches ?? false;
        IsReleaseBranch = currentBranchConfig.IsReleaseBranch ?? false;
        IsMainline = currentBranchConfig.IsMainline ?? false;
        CommitDateFormat = configuration.CommitDateFormat;
        UpdateBuildNumber = configuration.UpdateBuildNumber ?? true;
        SemanticVersionFormat = configuration.SemanticVersionFormat;
        PreReleaseWeight = currentBranchConfig.PreReleaseWeight ?? 0;
        TagPreReleaseWeight = configuration.TagPreReleaseWeight.Value;
    }

    protected EffectiveConfiguration(AssemblyVersioningScheme assemblyVersioningScheme,
        AssemblyFileVersioningScheme assemblyFileVersioningScheme,
        string? assemblyInformationalFormat,
        string? assemblyVersioningFormat,
        string? assemblyFileVersioningFormat,
        VersioningMode versioningMode,
        string? tagPrefix,
        string? tag,
        string? nextVersion,
        IncrementStrategy increment,
        string? branchPrefixToTrim,
        bool preventIncrementOfMergedBranchVersion,
        string? tagNumberPattern,
        string? continuousDeploymentFallbackTag,
        bool trackMergeTarget,
        string? majorVersionBumpMessage,
        string? minorVersionBumpMessage,
        string? patchVersionBumpMessage,
        string? noBumpMessage,
        CommitMessageIncrementMode commitMessageIncrementing,
        IEnumerable<IVersionFilter> versionFilters,
        bool tracksReleaseBranches,
        bool isReleaseBranch,
        bool isMainline,
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
        VersioningMode = versioningMode;
        TagPrefix = tagPrefix;
        Tag = tag;
        NextVersion = nextVersion;
        Increment = increment;
        BranchPrefixToTrim = branchPrefixToTrim;
        PreventIncrementOfMergedBranchVersion = preventIncrementOfMergedBranchVersion;
        TagNumberPattern = tagNumberPattern;
        ContinuousDeploymentFallbackTag = continuousDeploymentFallbackTag;
        TrackMergeTarget = trackMergeTarget;
        MajorVersionBumpMessage = majorVersionBumpMessage;
        MinorVersionBumpMessage = minorVersionBumpMessage;
        PatchVersionBumpMessage = patchVersionBumpMessage;
        NoBumpMessage = noBumpMessage;
        CommitMessageIncrementing = commitMessageIncrementing;
        VersionFilters = versionFilters;
        TracksReleaseBranches = tracksReleaseBranches;
        IsReleaseBranch = isReleaseBranch;
        IsMainline = isMainline;
        CommitDateFormat = commitDateFormat;
        UpdateBuildNumber = updateBuildNumber;
        SemanticVersionFormat = semanticVersionFormat;
        PreReleaseWeight = preReleaseWeight;
        TagPreReleaseWeight = tagPreReleaseWeight;
    }

    public bool TracksReleaseBranches { get; }
    public bool IsReleaseBranch { get; }
    public bool IsMainline { get; }
    public VersioningMode VersioningMode { get; }
    public AssemblyVersioningScheme AssemblyVersioningScheme { get; }
    public AssemblyFileVersioningScheme AssemblyFileVersioningScheme { get; }
    public string? AssemblyInformationalFormat { get; }
    public string? AssemblyVersioningFormat { get; }
    public string? AssemblyFileVersioningFormat { get; }

    /// <summary>
    ///     Git tag prefix
    /// </summary>
    public string? TagPrefix { get; }

    /// <summary>
    ///     Tag to use when calculating SemVer
    /// </summary>
    public string? Tag { get; }

    public string? NextVersion { get; }

    public IncrementStrategy Increment { get; }

    public string? BranchPrefixToTrim { get; }

    public bool PreventIncrementOfMergedBranchVersion { get; }

    public string? TagNumberPattern { get; }

    public string? ContinuousDeploymentFallbackTag { get; }

    public bool TrackMergeTarget { get; }

    public string? MajorVersionBumpMessage { get; }

    public string? MinorVersionBumpMessage { get; }

    public string? PatchVersionBumpMessage { get; }

    public string? NoBumpMessage { get; }

    public CommitMessageIncrementMode CommitMessageIncrementing { get; }

    public IEnumerable<IVersionFilter> VersionFilters { get; }

    public string? CommitDateFormat { get; }

    public bool UpdateBuildNumber { get; }

    public SemanticVersionFormat SemanticVersionFormat { get; set; } = SemanticVersionFormat.Strict;

    public int PreReleaseWeight { get; }

    public int TagPreReleaseWeight { get; }
}
