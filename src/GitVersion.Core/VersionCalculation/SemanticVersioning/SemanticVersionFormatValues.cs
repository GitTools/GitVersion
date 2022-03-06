using System.Globalization;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion;

public class SemanticVersionFormatValues
{
    private readonly SemanticVersion semver;
    private readonly EffectiveConfiguration config;

    public SemanticVersionFormatValues(SemanticVersion semver, EffectiveConfiguration config)
    {
        this.semver = semver;
        this.config = config;
    }

    public string Major => this.semver.Major.ToString();

    public string Minor => this.semver.Minor.ToString();

    public string Patch => this.semver.Patch.ToString();

    public string? PreReleaseTag => this.semver.PreReleaseTag;

    public string? PreReleaseTagWithDash => this.semver.PreReleaseTag?.HasTag() == true ? "-" + this.semver.PreReleaseTag : null;

    public string? PreReleaseLabel => this.semver.PreReleaseTag?.HasTag() == true ? this.semver.PreReleaseTag.Name : null;

    public string? PreReleaseLabelWithDash => this.semver.PreReleaseTag?.HasTag() == true ? "-" + this.semver.PreReleaseTag.Name : null;

    public string? PreReleaseNumber => this.semver.PreReleaseTag?.HasTag() == true ? this.semver.PreReleaseTag.Number.ToString() : null;

    public string WeightedPreReleaseNumber => GetWeightedPreReleaseNumber();

    public string? BuildMetaData => this.semver.BuildMetaData;

    public string? BuildMetaDataPadded => this.semver.BuildMetaData?.ToString("p" + this.config.BuildMetaDataPadding);

    public string? FullBuildMetaData => this.semver.BuildMetaData?.ToString("f");

    public string MajorMinorPatch => $"{this.semver.Major}.{this.semver.Minor}.{this.semver.Patch}";

    public string SemVer => this.semver.ToString();

    public string LegacySemVer => this.semver.ToString("l");

    public string LegacySemVerPadded => this.semver.ToString("lp" + this.config.LegacySemVerPadding);

    public string? AssemblySemVer => this.semver.GetAssemblyVersion(this.config.AssemblyVersioningScheme);

    public string? AssemblyFileSemVer => this.semver.GetAssemblyFileVersion(this.config.AssemblyFileVersioningScheme);

    public string FullSemVer => this.semver.ToString("f");

    public string? BranchName => this.semver.BuildMetaData?.Branch;

    public string? EscapedBranchName => this.semver.BuildMetaData?.Branch?.RegexReplace("[^a-zA-Z0-9-]", "-");

    public string? Sha => this.semver.BuildMetaData?.Sha;

    public string? ShortSha => this.semver.BuildMetaData?.ShortSha;

    public string? CommitDate => this.semver.BuildMetaData?.CommitDate?.UtcDateTime.ToString(this.config.CommitDateFormat, CultureInfo.InvariantCulture);

    // TODO When NuGet 3 is released: public string NuGetVersionV3 { get { return ??; } }

    public string NuGetVersionV2 => LegacySemVerPadded.ToLower();

    public string NuGetVersion => NuGetVersionV2;

    public string? NuGetPreReleaseTagV2 => this.semver.PreReleaseTag?.HasTag() == true ? this.semver.PreReleaseTag?.ToString("lp").ToLower() : null;

    public string? NuGetPreReleaseTag => NuGetPreReleaseTagV2;

    public string InformationalVersion => this.semver.ToString("i");

    [Obsolete("Use InformationalVersion instead")]
    public string DefaultInformationalVersion => InformationalVersion;

    public string? VersionSourceSha => this.semver.BuildMetaData?.VersionSourceSha;

    public string? CommitsSinceVersionSource => this.semver.BuildMetaData?.CommitsSinceVersionSource?.ToString(CultureInfo.InvariantCulture);

    public string? CommitsSinceVersionSourcePadded => this.semver.BuildMetaData?.CommitsSinceVersionSource?.ToString(CultureInfo.InvariantCulture).PadLeft(this.config.CommitsSinceVersionSourcePadding, '0');

    public string? UncommittedChanges => this.semver.BuildMetaData?.UncommittedChanges.ToString(CultureInfo.InvariantCulture);

    private string GetWeightedPreReleaseNumber()
    {
        var weightedPreReleaseNumber =
            this.semver.PreReleaseTag?.HasTag() == true ? (this.semver.PreReleaseTag.Number + this.config.PreReleaseWeight).ToString() : null;

        return weightedPreReleaseNumber.IsNullOrEmpty()
            ? $"{this.config.TagPreReleaseWeight}"
            : weightedPreReleaseNumber;
    }
}
