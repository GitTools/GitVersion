using System.Globalization;

namespace GitVersion
{
    public class SemanticVersionFormatValues
    {
        readonly SemanticVersion _semver;
        readonly EffectiveConfiguration _config;

        public SemanticVersionFormatValues(SemanticVersion semver, EffectiveConfiguration config)
        {
            _semver = semver;
            _config = config;
        }

        public string Major => _semver.Major.ToString();

        public string Minor => _semver.Minor.ToString();

        public string Patch => _semver.Patch.ToString();

        public string PreReleaseTag => _semver.PreReleaseTag;

        public string PreReleaseTagWithDash => _semver.PreReleaseTag.HasTag() ? "-" + _semver.PreReleaseTag : null;

        public string PreReleaseLabel => _semver.PreReleaseTag.HasTag() ? _semver.PreReleaseTag.Name : null;

        public string PreReleaseNumber => _semver.PreReleaseTag.HasTag() ? _semver.PreReleaseTag.Number.ToString() : null;

        public string WeightedPreReleaseNumber => _semver.PreReleaseTag.HasTag() ? (_semver.PreReleaseTag.Number + _config.PreReleaseWeight).ToString() : null;

        public string BuildMetaData => _semver.BuildMetaData;

        public string BuildMetaDataPadded => _semver.BuildMetaData.ToString("p" + _config.BuildMetaDataPadding);

        public string FullBuildMetaData => _semver.BuildMetaData.ToString("f");

        public string MajorMinorPatch => $"{_semver.Major}.{_semver.Minor}.{_semver.Patch}";

        public string SemVer => _semver.ToString();

        public string LegacySemVer => _semver.ToString("l");

        public string LegacySemVerPadded => _semver.ToString("lp" + _config.LegacySemVerPadding);

        public string AssemblySemVer => _semver.GetAssemblyVersion(_config.AssemblyVersioningScheme);

        public string AssemblyFileSemVer => _semver.GetAssemblyFileVersion(_config.AssemblyFileVersioningScheme);

        public string FullSemVer => _semver.ToString("f");

        public string BranchName => _semver.BuildMetaData.Branch;

        public string Sha => _semver.BuildMetaData.Sha;

        public string ShortSha => _semver.BuildMetaData.ShortSha;

        public string CommitDate => _semver.BuildMetaData.CommitDate.UtcDateTime.ToString(_config.CommitDateFormat, CultureInfo.InvariantCulture);

        // TODO When NuGet 3 is released: public string NuGetVersionV3 { get { return ??; } }

        public string NuGetVersionV2 => LegacySemVerPadded.ToLower();

        public string NuGetVersion => NuGetVersionV2;

        public string NuGetPreReleaseTagV2 => _semver.PreReleaseTag.HasTag() ? _semver.PreReleaseTag.ToString("lp").ToLower() : null;

        public string NuGetPreReleaseTag => NuGetPreReleaseTagV2;

        public string DefaultInformationalVersion => _semver.ToString("i");

        public string VersionSourceSha => _semver.BuildMetaData.VersionSourceSha;

        public string CommitsSinceVersionSource => _semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture);

        public string CommitsSinceVersionSourcePadded => _semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture).PadLeft(_config.CommitsSinceVersionSourcePadding, '0');
    }
}
