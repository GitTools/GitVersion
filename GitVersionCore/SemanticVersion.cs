namespace GitVersion
{
    using System;
    using System.Text.RegularExpressions;

    public class SemanticVersion : IFormattable, IComparable<SemanticVersion>
    {
        static readonly Regex ParseSemVer = new Regex(
            @"[vV]?(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?",
            RegexOptions.Compiled);

        public int Major;
        public int Minor;
        public int Patch;
        public SemanticVersionPreReleaseTag PreReleaseTag;
        public SemanticVersionBuildMetaData BuildMetaData;

        public SemanticVersion()
        {
            PreReleaseTag = new SemanticVersionPreReleaseTag();
            BuildMetaData = new SemanticVersionBuildMetaData();
        }

        public bool Equals(SemanticVersion obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Major == obj.Major &&
                   Minor == obj.Minor &&
                   Patch == obj.Patch &&
                   PreReleaseTag == obj.PreReleaseTag &&
                   BuildMetaData == obj.BuildMetaData;
        }

        public static bool operator ==(SemanticVersion v1, SemanticVersion v2)
        {
            if (ReferenceEquals(v1, null))
            {
                return ReferenceEquals(v2, null);
            }
            return v1.Equals(v2);
        }

        public static bool operator !=(SemanticVersion v1, SemanticVersion v2)
        {
            return !(v1 == v2);
        }

        public static bool operator >(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");
            return v1.CompareTo(v2) > 0;
        }

        public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");
            return v1.CompareTo(v2) >= 0;
        }

        public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");

            return v1.CompareTo(v2) <= 0;
        }

        public static bool operator <(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");

            return v1.CompareTo(v2) < 0;
        }

        public static SemanticVersion Parse(string version)
        {
            SemanticVersion semanticVersion;
            if (!TryParse(version, out semanticVersion))
                throw new WarningException(string.Format("Failed to parse {0} into a Semantic Version", version));

            return semanticVersion;
        }

        public static bool TryParse(string version, out SemanticVersion semanticVersion)
        {
            var parsed = ParseSemVer.Match(version);

            if (!parsed.Success)
            {
                semanticVersion = null;
                return false;
            }

            var semanticVersionBuildMetaData = SemanticVersionBuildMetaData.Parse(parsed.Groups["BuildMetaData"].Value);
            var fourthPart = parsed.Groups["FourthPart"];
            if (fourthPart.Success && semanticVersionBuildMetaData.CommitsSinceTag == null)
            {
                semanticVersionBuildMetaData.CommitsSinceTag = int.Parse(fourthPart.Value);
            }

            semanticVersion = new SemanticVersion
            {
                Major = int.Parse(parsed.Groups["Major"].Value),
                Minor = parsed.Groups["Minor"].Success ? int.Parse(parsed.Groups["Minor"].Value) : 0,
                Patch = parsed.Groups["Patch"].Success ? int.Parse(parsed.Groups["Patch"].Value) : 0,
                PreReleaseTag = SemanticVersionPreReleaseTag.Parse(parsed.Groups["Tag"].Value),
                BuildMetaData = semanticVersionBuildMetaData
            };

            return true;
        }

        public int CompareTo(SemanticVersion value)
        {
            if (value == null)
            {
                return 1;
            }
            if (Major != value.Major)
            {
                if (Major > value.Major)
                {
                    return 1;
                }
                return -1;
            }
            if (Minor != value.Minor)
            {
                if (Minor > value.Minor)
                {
                    return 1;
                }
                return -1;
            }
            if (Patch != value.Patch)
            {
                if (Patch > value.Patch)
                {
                    return 1;
                }
                return -1;
            }
            if (PreReleaseTag != value.PreReleaseTag)
            {
                if (PreReleaseTag > value.PreReleaseTag)
                {
                    return 1;
                }
                return -1;
            }

            return 0;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// <para>s - Default SemVer [1.2.3-beta.4+5]</para>
        /// <para>f - Full SemVer [1.2.3-beta.4+5]</para>
        /// <para>i - Informational SemVer [1.2.3-beta.4+5.Branch.master.BranchType.Master.Sha.000000]</para>
        /// <para>j - Just the SemVer part [1.2.3]</para>
        /// <para>t - SemVer with the tag [1.2.3-beta.4]</para>
        /// <para>l - Legacy SemVer tag for systems which do not support SemVer 2.0 properly [1.2.3-beta4]</para>
        /// <para>lp - Legacy SemVer tag for systems which do not support SemVer 2.0 properly (padded) [1.2.3-beta0004]</para>
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (string.IsNullOrEmpty(format))
                format = "s";

            if (formatProvider != null)
            {
                var formatter = formatProvider.GetFormat(GetType()) as ICustomFormatter;

                if (formatter != null)
                    return formatter.Format(format, this, formatProvider);
            }

            switch (format.ToLower())
            {
                case "j":
                    return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
                case "s":
                    return PreReleaseTag.HasTag() ? string.Format("{0}-{1}", ToString("j"), PreReleaseTag) : ToString("j");
                case "l":
                    return PreReleaseTag.HasTag() ? string.Format("{0}-{1}", ToString("j"), PreReleaseTag.ToString("l")) : ToString("j");
                case "lp":
                    return PreReleaseTag.HasTag() ? string.Format("{0}-{1}", ToString("j"), PreReleaseTag.ToString("lp")) : ToString("j");
                case "f":
                    {
                        var buildMetadata = BuildMetaData.ToString();

                        return !string.IsNullOrEmpty(buildMetadata) ? string.Format("{0}+{1}", ToString("s"), buildMetadata) : ToString("s");
                    }
                case "i":
                    {
                        var buildMetadata = BuildMetaData.ToString("f");

                        return !string.IsNullOrEmpty(buildMetadata) ? string.Format("{0}+{1}", ToString("s"), buildMetadata) : ToString("s");
                    }
                default:
                    throw new ArgumentException(string.Format("Unrecognised format '{0}'", format), "format");
            }
        }
    }
}