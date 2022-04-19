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
        Configuration = configuration.NotNull();
        currentBranchConfig.NotNull();

        var name = currentBranchConfig.Name;

        if (!currentBranchConfig.VersioningMode.HasValue)
            throw new Exception($"Configuration value for 'Versioning mode' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!currentBranchConfig.Increment.HasValue)
            throw new Exception($"Configuration value for 'Increment' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!currentBranchConfig.PreventIncrementOfMergedBranchVersion.HasValue)
            throw new Exception($"Configuration value for 'PreventIncrementOfMergedBranchVersion' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!currentBranchConfig.TrackMergeTarget.HasValue)
            throw new Exception($"Configuration value for 'TrackMergeTarget' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!currentBranchConfig.TracksReleaseBranches.HasValue)
            throw new Exception($"Configuration value for 'TracksReleaseBranches' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!currentBranchConfig.IsReleaseBranch.HasValue)
            throw new Exception($"Configuration value for 'IsReleaseBranch' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!configuration.AssemblyVersioningScheme.HasValue)
            throw new Exception("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");

        if (!configuration.AssemblyFileVersioningScheme.HasValue)
            throw new Exception("Configuration value for 'AssemblyFileVersioningScheme' has no value. (this should not happen, please report an issue)");

        if (!configuration.CommitMessageIncrementing.HasValue)
            throw new Exception("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");

        if (!configuration.LegacySemVerPadding.HasValue)
            throw new Exception("Configuration value for 'LegacySemVerPadding' has no value. (this should not happen, please report an issue)");

        if (!configuration.BuildMetaDataPadding.HasValue)
            throw new Exception("Configuration value for 'BuildMetaDataPadding' has no value. (this should not happen, please report an issue)");

        if (!configuration.CommitsSinceVersionSourcePadding.HasValue)
            throw new Exception("Configuration value for 'CommitsSinceVersionSourcePadding' has no value. (this should not happen, please report an issue)");

        if (!configuration.TagPreReleaseWeight.HasValue)
            throw new Exception("Configuration value for 'TagPreReleaseWeight' has no value. (this should not happen, please report an issue)");

        AssemblyVersioningScheme = configuration.AssemblyVersioningScheme.Value;
        AssemblyFileVersioningScheme = configuration.AssemblyFileVersioningScheme.Value;
        AssemblyInformationalFormat = configuration.AssemblyInformationalFormat;
        AssemblyVersioningFormat = configuration.AssemblyVersioningFormat;
        AssemblyFileVersioningFormat = configuration.AssemblyFileVersioningFormat;
        VersioningMode = currentBranchConfig.VersioningMode.Value;
        GitTagPrefix = configuration.TagPrefix;
        Tag = currentBranchConfig.Tag;
        NextVersion = configuration.NextVersion;
        Increment = currentBranchConfig.Increment.Value;
        BranchPrefixToTrim = currentBranchConfig.Regex;
        PreventIncrementForMergedBranchVersion = currentBranchConfig.PreventIncrementOfMergedBranchVersion.Value;
        TagNumberPattern = currentBranchConfig.TagNumberPattern;
        ContinuousDeploymentFallbackTag = configuration.ContinuousDeploymentFallbackTag;
        TrackMergeTarget = currentBranchConfig.TrackMergeTarget.Value;
        MajorVersionBumpMessage = configuration.MajorVersionBumpMessage;
        MinorVersionBumpMessage = configuration.MinorVersionBumpMessage;
        PatchVersionBumpMessage = configuration.PatchVersionBumpMessage;
        NoBumpMessage = configuration.NoBumpMessage;
        CommitMessageIncrementing = currentBranchConfig.CommitMessageIncrementing ?? configuration.CommitMessageIncrementing.Value;
        LegacySemVerPadding = configuration.LegacySemVerPadding.Value;
        BuildMetaDataPadding = configuration.BuildMetaDataPadding.Value;
        CommitsSinceVersionSourcePadding = configuration.CommitsSinceVersionSourcePadding.Value;
        VersionFilters = configuration.Ignore.ToFilters();
        TracksReleaseBranches = currentBranchConfig.TracksReleaseBranches.Value;
        IsCurrentBranchRelease = currentBranchConfig.IsReleaseBranch.Value;
        CommitDateFormat = configuration.CommitDateFormat;
        UpdateBuildNumber = configuration.UpdateBuildNumber ?? true;
        PreReleaseWeight = currentBranchConfig.PreReleaseWeight ?? 0;
        TagPreReleaseWeight = configuration.TagPreReleaseWeight.Value;
    }

    protected EffectiveConfiguration(AssemblyVersioningScheme assemblyVersioningScheme,
        AssemblyFileVersioningScheme assemblyFileVersioningScheme,
        string? assemblyInformationalFormat,
        string? assemblyVersioningFormat,
        string? assemblyFileVersioningFormat,
        VersioningMode versioningMode,
        string? gitTagPrefix,
        string? tag,
        string? nextVersion,
        IncrementStrategy increment,
        string? branchPrefixToTrim,
        bool preventIncrementForMergedBranchVersion,
        string? tagNumberPattern,
        string? continuousDeploymentFallbackTag,
        bool trackMergeTarget,
        string? majorVersionBumpMessage,
        string? minorVersionBumpMessage,
        string? patchVersionBumpMessage,
        string? noBumpMessage,
        CommitMessageIncrementMode commitMessageIncrementing,
        int legacySemVerPaddding,
        int buildMetaDataPadding,
        int commitsSinceVersionSourcePadding,
        IEnumerable<IVersionFilter> versionFilters,
        bool tracksReleaseBranches,
        bool isCurrentBranchRelease,
        string? commitDateFormat,
        bool updateBuildNumber,
        int preReleaseWeight,
        int tagPreReleaseWeight)
    {
        AssemblyVersioningScheme = assemblyVersioningScheme;
        AssemblyFileVersioningScheme = assemblyFileVersioningScheme;
        AssemblyInformationalFormat = assemblyInformationalFormat;
        AssemblyVersioningFormat = assemblyVersioningFormat;
        AssemblyFileVersioningFormat = assemblyFileVersioningFormat;
        VersioningMode = versioningMode;
        GitTagPrefix = gitTagPrefix;
        Tag = tag;
        NextVersion = nextVersion;
        Increment = increment;
        BranchPrefixToTrim = branchPrefixToTrim;
        PreventIncrementForMergedBranchVersion = preventIncrementForMergedBranchVersion;
        TagNumberPattern = tagNumberPattern;
        ContinuousDeploymentFallbackTag = continuousDeploymentFallbackTag;
        TrackMergeTarget = trackMergeTarget;
        MajorVersionBumpMessage = majorVersionBumpMessage;
        MinorVersionBumpMessage = minorVersionBumpMessage;
        PatchVersionBumpMessage = patchVersionBumpMessage;
        NoBumpMessage = noBumpMessage;
        CommitMessageIncrementing = commitMessageIncrementing;
        LegacySemVerPadding = legacySemVerPaddding;
        BuildMetaDataPadding = buildMetaDataPadding;
        CommitsSinceVersionSourcePadding = commitsSinceVersionSourcePadding;
        VersionFilters = versionFilters;
        TracksReleaseBranches = tracksReleaseBranches;
        IsCurrentBranchRelease = isCurrentBranchRelease;
        CommitDateFormat = commitDateFormat;
        UpdateBuildNumber = updateBuildNumber;
        PreReleaseWeight = preReleaseWeight;
        TagPreReleaseWeight = tagPreReleaseWeight;
    }

    public bool TracksReleaseBranches { get; }
    public bool IsCurrentBranchRelease { get; }
    public VersioningMode VersioningMode { get; }
    public AssemblyVersioningScheme AssemblyVersioningScheme { get; }
    public AssemblyFileVersioningScheme AssemblyFileVersioningScheme { get; }
    public string? AssemblyInformationalFormat { get; }
    public string? AssemblyVersioningFormat { get; }
    public string? AssemblyFileVersioningFormat { get; }

    /// <summary>
    ///     Git tag prefix
    /// </summary>
    public string? GitTagPrefix { get; }

    /// <summary>
    ///     Tag to use when calculating SemVer
    /// </summary>
    public string? Tag { get; }

    public string? NextVersion { get; }

    public IncrementStrategy Increment { get; }

    public string? BranchPrefixToTrim { get; }

    public bool PreventIncrementForMergedBranchVersion { get; }

    public string? TagNumberPattern { get; }

    public string? ContinuousDeploymentFallbackTag { get; }

    public bool TrackMergeTarget { get; }

    public string? MajorVersionBumpMessage { get; }

    public string? MinorVersionBumpMessage { get; }

    public string? PatchVersionBumpMessage { get; }

    public string? NoBumpMessage { get; }
    public int LegacySemVerPadding { get; }
    public int BuildMetaDataPadding { get; }

    public int CommitsSinceVersionSourcePadding { get; }

    public CommitMessageIncrementMode CommitMessageIncrementing { get; }

    public IEnumerable<IVersionFilter> VersionFilters { get; }

    public string? CommitDateFormat { get; }

    public bool UpdateBuildNumber { get; }

    public int PreReleaseWeight { get; }

    public int TagPreReleaseWeight { get; }

    public Config Configuration { get; }
}
