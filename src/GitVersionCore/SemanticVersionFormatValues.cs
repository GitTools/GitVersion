namespace GitVersion
{
    using System.Globalization;

    public class SemanticVersionFormatValues
    {
        readonly SemanticVersion _semver;
        readonly EffectiveConfiguration _config;

        public SemanticVersionFormatValues(SemanticVersion semver, EffectiveConfiguration config)
        {
            _semver = semver;
            _config = config;
        }

        public string Major
        {
            get { return _semver.Major.ToString(); }
        }

        public string Minor
        {
            get { return _semver.Minor.ToString(); }
        }

        public string Patch
        {
            get { return _semver.Patch.ToString(); }
        }

        public string PreReleaseTag
        {
            get { return _semver.PreReleaseTag; }
        }

        public string PreReleaseTagWithDash
        {
            get { return _semver.PreReleaseTag.HasTag() ? "-" + _semver.PreReleaseTag : null; }
        }

        public string PreReleaseLabel
        {
            get { return _semver.PreReleaseTag.HasTag() ? _semver.PreReleaseTag.Name : null; }
        }

        public string PreReleaseNumber
        {
            get { return _semver.PreReleaseTag.HasTag() ? _semver.PreReleaseTag.Number.ToString() : null; }
        }

        public string WeightedPreReleaseNumber
        {
            get { return _semver.PreReleaseTag.HasTag() ? (_semver.PreReleaseTag.Number + _config.PreReleaseWeight).ToString() : null; }
        }

        public string BuildMetaData
        {
            get { return _semver.BuildMetaData; }
        }

        public string BuildMetaDataPadded
        {
            get { return _semver.BuildMetaData.ToString("p" + _config.BuildMetaDataPadding); }
        }

        public string FullBuildMetaData
        {
            get { return _semver.BuildMetaData.ToString("f"); }
        }

        public string MajorMinorPatch
        {
            get { return string.Format("{0}.{1}.{2}", _semver.Major, _semver.Minor, _semver.Patch); }
        }

        public string SemVer
        {
            get { return _semver.ToString(); }
        }

        public string LegacySemVer
        {
            get { return _semver.ToString("l"); }
        }

        public string LegacySemVerPadded
        {
            get { return _semver.ToString("lp" + _config.LegacySemVerPadding); }
        }

        public string AssemblySemVer
        {
            get { return _semver.GetAssemblyVersion(_config.AssemblyVersioningScheme); }
        }
        public string AssemblyFileSemVer
        {
            get { return _semver.GetAssemblyFileVersion(_config.AssemblyFileVersioningScheme); }
        }

        public string FullSemVer
        {
            get { return _semver.ToString("f"); }
        }

        public string BranchName
        {
            get { return _semver.BuildMetaData.Branch; }
        }

        public string Sha
        {
            get { return _semver.BuildMetaData.Sha; }
        }

        public string ShortSha
        {
            get { return _semver.BuildMetaData.ShortSha; }
        }

        public string CommitDate
        {
            get { return _semver.BuildMetaData.CommitDate.UtcDateTime.ToString(_config.CommitDateFormat, CultureInfo.InvariantCulture); }
        }

        // TODO When NuGet 3 is released: public string NuGetVersionV3 { get { return ??; } }

        public string NuGetVersionV2
        {
            get { return LegacySemVerPadded.ToLower(); }
        }

        public string NuGetVersion
        {
            get { return NuGetVersionV2; }
        }

        public string NuGetPreReleaseTagV2
        {
            get { return _semver.PreReleaseTag.HasTag() ? _semver.PreReleaseTag.ToString("lp").ToLower() : null; }
        }

        public string NuGetPreReleaseTag
        {
            get { return NuGetPreReleaseTagV2; }
        }

        public string DefaultInformationalVersion
        {
            get { return _semver.ToString("i"); }
        }

        public string VersionSourceSha
        {
            get { return _semver.BuildMetaData.VersionSourceSha; }
        }

        public string CommitsSinceVersionSource
        {
            get { return _semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture); }
        }

        public string CommitsSinceVersionSourcePadded
        {
            get { return _semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture).PadLeft(_config.CommitsSinceVersionSourcePadding, '0'); }
        }
    }
}
