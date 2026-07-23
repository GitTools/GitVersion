using System.Globalization;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion;

/// <summary>Exposes all computed version format values used by custom format strings in GitVersion configuration.</summary>
public class SemanticVersionFormatValues(SemanticVersion semver, IGitVersionConfiguration configuration, int preReleaseWeight)
{
    /// <summary>Gets the major version component as a string.</summary>
    public string Major => semver.Major.ToString();

    /// <summary>Gets the minor version component as a string.</summary>
    public string Minor => semver.Minor.ToString();

    /// <summary>Gets the patch version component as a string.</summary>
    public string Patch => semver.Patch.ToString();

    /// <summary>Gets the full pre-release tag (e.g. <c>beta.1</c>).</summary>
    public string PreReleaseTag => semver.PreReleaseTag.ToString();

    /// <summary>Gets the pre-release tag prefixed with a dash (e.g. <c>-beta.1</c>), or empty string when there is no tag.</summary>
    public string PreReleaseTagWithDash => PreReleaseTag.WithPrefixIfNotNullOrEmpty("-");

    /// <summary>Gets the pre-release label without the numeric identifier (e.g. <c>beta</c>).</summary>
    public string PreReleaseLabel => semver.PreReleaseTag.Name;

    /// <summary>Gets the pre-release label prefixed with a dash (e.g. <c>-beta</c>), or empty string when there is no label.</summary>
    public string PreReleaseLabelWithDash => PreReleaseLabel.WithPrefixIfNotNullOrEmpty("-");

    /// <summary>Gets the numeric pre-release identifier as a string, or empty string when absent.</summary>
    public string PreReleaseNumber => semver.PreReleaseTag.Number?.ToString() ?? string.Empty;

    /// <summary>Gets the pre-release number adjusted by the configured pre-release weight.</summary>
    public string WeightedPreReleaseNumber => semver.PreReleaseTag.Number.HasValue
        ? $"{semver.PreReleaseTag.Number.Value + preReleaseWeight}" : $"{configuration.TagPreReleaseWeight}";

    /// <summary>Gets the build metadata string (commits-since-tag).</summary>
    public string BuildMetaData => semver.BuildMetaData.ToString();

    /// <summary>Gets the full build metadata string including branch, SHA, and other fields.</summary>
    public string FullBuildMetaData => semver.BuildMetaData.ToString("f");

    /// <summary>Gets the version in <c>Major.Minor.Patch</c> format.</summary>
    public string MajorMinorPatch => $"{semver.Major}.{semver.Minor}.{semver.Patch}";

    /// <summary>Gets the default semantic version string (without build metadata).</summary>
    public string SemVer => semver.ToString();

    /// <summary>Gets the assembly version string computed according to the configured <see cref="AssemblyVersioningScheme"/>.</summary>
    public string? AssemblySemVer => semver.GetAssemblyVersion(configuration.AssemblyVersioningScheme!.Value);

    /// <summary>Gets the assembly file version string computed according to the configured <see cref="AssemblyFileVersioningScheme"/>.</summary>
    public string? AssemblyFileSemVer => semver.GetAssemblyFileVersion(configuration.AssemblyFileVersioningScheme!.Value);

    /// <summary>Gets the full semantic version string including build metadata.</summary>
    public string FullSemVer => semver.ToString("f");

    /// <summary>Gets the name of the branch on which this version was calculated.</summary>
    public string? BranchName => semver.BuildMetaData.Branch;

    /// <summary>Gets the branch name with characters that are invalid in environment variable names replaced by dashes.</summary>
    public string? EscapedBranchName => semver.BuildMetaData.Branch?.RegexReplace(RegexPatterns.SanitizeNameRegexPattern, "-");

    /// <summary>Gets the full SHA of the current commit.</summary>
    public string? Sha => semver.BuildMetaData.Sha;

    /// <summary>Gets the abbreviated SHA of the current commit.</summary>
    public string? ShortSha => semver.BuildMetaData.ShortSha;

    /// <summary>Gets the commit date formatted according to the configured <c>CommitDateFormat</c>.</summary>
    public string? CommitDate => semver.BuildMetaData.CommitDate?.UtcDateTime.ToString(configuration.CommitDateFormat, CultureInfo.InvariantCulture);

    /// <summary>Gets the informational version string (SemVer + full build metadata).</summary>
    public string InformationalVersion => semver.ToString("i");

    /// <summary>Gets the semantic version of the source tag from which the version was calculated.</summary>
    public string? VersionSourceSemVer => semver.BuildMetaData.VersionSourceSemVer?.ToString();

    /// <summary>Gets the SHA of the source tag commit.</summary>
    public string? VersionSourceSha => semver.BuildMetaData.VersionSourceSha;

    /// <summary>Gets the number of commits between the version source tag and the current commit.</summary>
    public string VersionSourceDistance => semver.BuildMetaData.VersionSourceDistance.ToString(CultureInfo.InvariantCulture);

    /// <summary>Gets the number of uncommitted changes in the working tree.</summary>
    public string UncommittedChanges => semver.BuildMetaData.UncommittedChanges.ToString(CultureInfo.InvariantCulture);

    /// <summary>Gets the version field that was incremented relative to the version source.</summary>
    public string VersionSourceIncrement => semver.BuildMetaData.VersionSourceIncrement.ToString();
}
