using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

/// <summary>Represents the top-level GitVersion configuration, extending branch-level configuration with global settings.</summary>
public interface IGitVersionConfiguration : IBranchConfiguration
{
    /// <summary>Gets the name of the workflow preset (e.g. <c>GitFlow/v1</c> or <c>GitHubFlow/v1</c>) used as a base configuration.</summary>
    string? Workflow { get; }

    /// <summary>Gets the scheme used to compute the <c>AssemblyVersionAttribute</c> value.</summary>
    AssemblyVersioningScheme? AssemblyVersioningScheme { get; }

    /// <summary>Gets the scheme used to compute the <c>AssemblyFileVersionAttribute</c> value.</summary>
    AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; }

    /// <summary>Gets the format string used to compute the <c>AssemblyInformationalVersionAttribute</c> value.</summary>
    string? AssemblyInformationalFormat { get; }

    /// <summary>Gets the format string used to compute the assembly version.</summary>
    string? AssemblyVersioningFormat { get; }

    /// <summary>Gets the format string used to compute the assembly file version.</summary>
    string? AssemblyFileVersioningFormat { get; }

    /// <summary>Gets the format string used to compute the custom version output.</summary>
    string? CustomVersionFormat { get; }

    /// <summary>Gets the regex pattern that identifies tag prefixes to strip when parsing version tags.</summary>
    string? TagPrefixPattern { get; }

    /// <summary>Gets the regex pattern used to extract a semantic version from a branch name.</summary>
    string? VersionInBranchPattern { get; }

    /// <summary>Gets the manually configured next version, overriding automatic calculation.</summary>
    string? NextVersion { get; }

    /// <summary>Gets the commit message pattern that triggers a major version bump.</summary>
    string? MajorVersionBumpMessage { get; }

    /// <summary>Gets the commit message pattern that triggers a minor version bump.</summary>
    string? MinorVersionBumpMessage { get; }

    /// <summary>Gets the commit message pattern that triggers a patch version bump.</summary>
    string? PatchVersionBumpMessage { get; }

    /// <summary>Gets the commit message pattern that suppresses any version bump.</summary>
    string? NoBumpMessage { get; }

    /// <summary>Gets the pre-release weight applied to tagged commits when calculating the weighted pre-release number.</summary>
    int? TagPreReleaseWeight { get; }

    /// <summary>Gets the format string used when rendering commit dates in version output.</summary>
    string? CommitDateFormat { get; }

    /// <summary>Gets a dictionary of named regex patterns for recognising merge commit message formats.</summary>
    IReadOnlyDictionary<string, string> MergeMessageFormats { get; }

    /// <summary>Gets a value indicating whether the build number in the CI system should be updated.</summary>
    bool UpdateBuildNumber { get; }

    /// <summary>Gets the semantic version format (strict or loose) used when parsing version strings.</summary>
    SemanticVersionFormat SemanticVersionFormat { get; }

    /// <summary>Gets the set of version strategies enabled for this configuration.</summary>
    VersionStrategies VersionStrategy { get; }

    /// <summary>Gets the per-branch configuration map, keyed by branch name pattern.</summary>
    IReadOnlyDictionary<string, IBranchConfiguration> Branches { get; }

    /// <summary>Gets the configuration that controls which commits and time ranges are ignored during version calculation.</summary>
    IIgnoreConfiguration Ignore { get; }

    /// <summary>Returns an empty branch configuration that carries no values.</summary>
    IBranchConfiguration GetEmptyBranchConfiguration();
}
