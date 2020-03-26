using System;
using System.Globalization;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion
{
    public class SemanticVersionFormatValues
    {
        private readonly SemanticVersion semver;
        private readonly EffectiveConfiguration config;

        public SemanticVersionFormatValues(SemanticVersion semver, EffectiveConfiguration config)
        {
            this.semver = semver;
            this.config = config;
        }

        public string Major => semver.Major.ToString();

        public string Minor => semver.Minor.ToString();

        public string Patch => semver.Patch.ToString();

        public string PreReleaseTag => semver.PreReleaseTag;

        public string PreReleaseTagWithDash => semver.PreReleaseTag.HasTag() ? "-" + semver.PreReleaseTag : null;

        public string PreReleaseLabel => semver.PreReleaseTag.HasTag() ? semver.PreReleaseTag.Name : null;

        public string PreReleaseNumber => semver.PreReleaseTag.HasTag() ? semver.PreReleaseTag.Number.ToString() : null;

        public string WeightedPreReleaseNumber => semver.PreReleaseTag.HasTag() ? (semver.PreReleaseTag.Number + config.PreReleaseWeight).ToString() : null;

        public string BuildMetaData => semver.BuildMetaData;

        public string BuildMetaDataPadded => semver.BuildMetaData.ToString("p" + config.BuildMetaDataPadding);

        public string FullBuildMetaData => semver.BuildMetaData.ToString("f");

        public string MajorMinorPatch => $"{semver.Major}.{semver.Minor}.{semver.Patch}";

        public string SemVer => semver.ToString();

        public string LegacySemVer => semver.ToString("l");

        public string LegacySemVerPadded => semver.ToString("lp" + config.LegacySemVerPadding);

        public string AssemblySemVer => semver.GetAssemblyVersion(config.AssemblyVersioningScheme);

        public string AssemblyFileSemVer => semver.GetAssemblyFileVersion(config.AssemblyFileVersioningScheme);

        public string FullSemVer => semver.ToString("f");

        public string BranchName => semver.BuildMetaData.Branch;

        public string EscapedBranchName => semver.BuildMetaData.Branch?.RegexReplace("[^a-zA-Z0-9-]", "-");

        public string Sha => semver.BuildMetaData.Sha;

        public string ShortSha => semver.BuildMetaData.ShortSha;

        public string CommitDate => semver.BuildMetaData.CommitDate.UtcDateTime.ToString(config.CommitDateFormat, CultureInfo.InvariantCulture);

        // TODO When NuGet 3 is released: public string NuGetVersionV3 { get { return ??; } }

        public string NuGetVersionV2 => LegacySemVerPadded.ToLower();

        public string NuGetVersion => NuGetVersionV2;

        public string NuGetPreReleaseTagV2 => semver.PreReleaseTag.HasTag() ? semver.PreReleaseTag.ToString("lp").ToLower() : null;

        public string NuGetPreReleaseTag => NuGetPreReleaseTagV2;

        public string InformationalVersion => semver.ToString("i");

        [Obsolete("Use InformationalVersion instead")]
        public string DefaultInformationalVersion => InformationalVersion;

        public string VersionSourceSha => semver.BuildMetaData.VersionSourceSha;

        public string CommitsSinceVersionSource => semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture);

        public string CommitsSinceVersionSourcePadded => semver.BuildMetaData.CommitsSinceVersionSource.ToString(CultureInfo.InvariantCulture).PadLeft(config.CommitsSinceVersionSourcePadding, '0');
    }
}
