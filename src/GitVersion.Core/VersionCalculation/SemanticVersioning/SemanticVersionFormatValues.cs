using System.Globalization;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;

namespace GitVersion;

public class SemanticVersionFormatValues(SemanticVersion semver, IGitVersionConfiguration configuration, int preReleaseWeight)
{
    public string Major => semver.Major.ToString();

    public string Minor => semver.Minor.ToString();

    public string Patch => semver.Patch.ToString();

    public string PreReleaseTag => semver.PreReleaseTag.ToString();

    public string PreReleaseTagWithDash => this.PreReleaseTag.WithPrefixIfNotNullOrEmpty("-");

    public string PreReleaseLabel => semver.PreReleaseTag.Name;

    public string PreReleaseLabelWithDash => this.PreReleaseLabel.WithPrefixIfNotNullOrEmpty("-");

    public string PreReleaseNumber => semver.PreReleaseTag.Number?.ToString() ?? string.Empty;

    public string WeightedPreReleaseNumber => semver.PreReleaseTag.Number.HasValue
        ? $"{semver.PreReleaseTag.Number.Value + preReleaseWeight}" : $"{configuration.TagPreReleaseWeight}";

    public string BuildMetaData => semver.BuildMetaData.ToString();

    public string FullBuildMetaData => semver.BuildMetaData.ToString("f");

    public string MajorMinorPatch => $"{semver.Major}.{semver.Minor}.{semver.Patch}";

    public string SemVer => semver.ToString();

    public string? AssemblySemVer => semver.GetAssemblyVersion(configuration.AssemblyVersioningScheme!.Value);

    public string? AssemblyFileSemVer => semver.GetAssemblyFileVersion(configuration.AssemblyFileVersioningScheme!.Value);

    public string FullSemVer => semver.ToString("f");

    public string? BranchName => semver.BuildMetaData.Branch;

    public string? EscapedBranchName => semver.BuildMetaData.Branch?.RegexReplace(RegexPatterns.Common.SanitizeNameRegexPattern, "-");

    public string? Sha => semver.BuildMetaData.Sha;

    public string? ShortSha => semver.BuildMetaData.ShortSha;

    public string? CommitDate => semver.BuildMetaData.CommitDate?.UtcDateTime.ToString(configuration.CommitDateFormat, CultureInfo.InvariantCulture);

    public string InformationalVersion => semver.ToString("i");

    public string CustomVersion => semver.ToString();

    public string? VersionSourceSha => semver.BuildMetaData.VersionSourceSha;

    public string CommitsSinceVersionSource => semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture);

    public string UncommittedChanges => semver.BuildMetaData.UncommittedChanges.ToString(CultureInfo.InvariantCulture);
}
