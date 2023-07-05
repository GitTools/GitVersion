using System.Globalization;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion;

public class SemanticVersionFormatValues
{
    private readonly SemanticVersion semver;
    private readonly EffectiveConfiguration configuration;

    public SemanticVersionFormatValues(SemanticVersion semver, EffectiveConfiguration configuration)
    {
        this.semver = semver;
        this.configuration = configuration;
    }

    public string Major => this.semver.Major.ToString();

    public string Minor => this.semver.Minor.ToString();

    public string Patch => this.semver.Patch.ToString();

    public string PreReleaseTag => this.semver.PreReleaseTag.ToString();

    public string PreReleaseTagWithDash => this.PreReleaseTag.WithPrefixIfNotNullOrEmpty("-");

    public string PreReleaseLabel => this.semver.PreReleaseTag.Name;

    public string PreReleaseLabelWithDash => this.PreReleaseLabel.WithPrefixIfNotNullOrEmpty("-");

    public string PreReleaseNumber => this.semver.PreReleaseTag.Number?.ToString() ?? string.Empty;

    public string WeightedPreReleaseNumber => GetWeightedPreReleaseNumber();

    public string BuildMetaData => this.semver.BuildMetaData.ToString();

    public string FullBuildMetaData => this.semver.BuildMetaData.ToString("f");

    public string MajorMinorPatch => $"{this.semver.Major}.{this.semver.Minor}.{this.semver.Patch}";

    public string SemVer => this.semver.ToString();

    public string? AssemblySemVer => this.semver.GetAssemblyVersion(this.configuration.AssemblyVersioningScheme);

    public string? AssemblyFileSemVer => this.semver.GetAssemblyFileVersion(this.configuration.AssemblyFileVersioningScheme);

    public string FullSemVer => this.semver.ToString("f");

    public string? BranchName => this.semver.BuildMetaData.Branch;

    public string? EscapedBranchName => this.semver.BuildMetaData.Branch?.RegexReplace("[^a-zA-Z0-9-]", "-");

    public string? Sha => this.semver.BuildMetaData.Sha;

    public string? ShortSha => this.semver.BuildMetaData.ShortSha;

    public string? CommitDate => this.semver.BuildMetaData.CommitDate?.UtcDateTime.ToString(this.configuration.CommitDateFormat, CultureInfo.InvariantCulture);

    public string InformationalVersion => this.semver.ToString("i");

    public string? VersionSourceSha => this.semver.BuildMetaData.VersionSourceSha;

    public string CommitsSinceVersionSource => this.semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture);

    public string UncommittedChanges => this.semver.BuildMetaData.UncommittedChanges.ToString(CultureInfo.InvariantCulture);

    private string GetWeightedPreReleaseNumber()
    {
        var weightedPreReleaseNumber =
            this.semver.PreReleaseTag.HasTag() ? (this.semver.PreReleaseTag.Number + this.configuration.PreReleaseWeight).ToString() : null;

        return weightedPreReleaseNumber.IsNullOrEmpty()
            ? $"{this.configuration.TagPreReleaseWeight}"
            : weightedPreReleaseNumber;
    }
}
