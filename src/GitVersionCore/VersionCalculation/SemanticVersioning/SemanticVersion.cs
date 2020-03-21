using System;
using System.Text.RegularExpressions;

namespace GitVersion
{
    public class SemanticVersion : IFormattable, IComparable<SemanticVersion>
    {
        private static SemanticVersion Empty = new SemanticVersion();

        private static readonly Regex ParseSemVer = new Regex(
            @"^(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?$",
            RegexOptions.Compiled);

        public int Major;
        public int Minor;
        public int Patch;
        public SemanticVersionPreReleaseTag PreReleaseTag;
        public SemanticVersionBuildMetaData BuildMetaData;

        public SemanticVersion(int major = 0, int minor = 0, int patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            PreReleaseTag = new SemanticVersionPreReleaseTag();
            BuildMetaData = new SemanticVersionBuildMetaData();
        }

        public SemanticVersion(SemanticVersion semanticVersion)
        {
            Major = semanticVersion.Major;
            Minor = semanticVersion.Minor;
            Patch = semanticVersion.Patch;

            PreReleaseTag = new SemanticVersionPreReleaseTag(semanticVersion.PreReleaseTag);
            BuildMetaData = new SemanticVersionBuildMetaData(semanticVersion.BuildMetaData);
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

        public bool IsEmpty()
        {
            return Equals(Empty);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((SemanticVersion)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                hashCode = (hashCode * 397) ^ (PreReleaseTag != null ? PreReleaseTag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BuildMetaData != null ? BuildMetaData.GetHashCode() : 0);
                return hashCode;
            }
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
                throw new ArgumentNullException(nameof(v1));
            if (v2 == null)
                throw new ArgumentNullException(nameof(v2));
            return v1.CompareTo(v2) > 0;
        }

        public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException(nameof(v1));
            if (v2 == null)
                throw new ArgumentNullException(nameof(v2));
            return v1.CompareTo(v2) >= 0;
        }

        public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException(nameof(v1));
            if (v2 == null)
                throw new ArgumentNullException(nameof(v2));

            return v1.CompareTo(v2) <= 0;
        }

        public static bool operator <(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException(nameof(v1));
            if (v2 == null)
                throw new ArgumentNullException(nameof(v2));

            return v1.CompareTo(v2) < 0;
        }

        public static SemanticVersion Parse(string version, string tagPrefixRegex)
        {
            if (!TryParse(version, tagPrefixRegex, out var semanticVersion))
                throw new WarningException($"Failed to parse {version} into a Semantic Version");

            return semanticVersion;
        }

        public static bool TryParse(string version, string tagPrefixRegex, out SemanticVersion semanticVersion)
        {
            var match = Regex.Match(version, $"^({tagPrefixRegex})?(?<version>.*)$");

            if (!match.Success)
            {
                semanticVersion = null;
                return false;
            }

            version = match.Groups["version"].Value;
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
            return CompareTo(value, true);
        }

        public int CompareTo(SemanticVersion value, bool includePrerelease)
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
            if (includePrerelease && PreReleaseTag != value.PreReleaseTag)
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
                if (formatProvider.GetFormat(GetType()) is ICustomFormatter formatter)
                    return formatter.Format(format, this, formatProvider);
            }

            // Check for lp first because the param can vary
            format = format.ToLower();
            if (format.StartsWith("lp", StringComparison.Ordinal))
            {
                // handle the padding
                return PreReleaseTag.HasTag() ? $"{ToString("j")}-{PreReleaseTag.ToString(format)}" : ToString("j");
            }

            switch (format)
            {
                case "j":
                    return $"{Major}.{Minor}.{Patch}";
                case "s":
                    return PreReleaseTag.HasTag() ? $"{ToString("j")}-{PreReleaseTag}" : ToString("j");
                case "t":
                    return PreReleaseTag.HasTag() ? $"{ToString("j")}-{PreReleaseTag.ToString("t")}" : ToString("j");
                case "l":
                    return PreReleaseTag.HasTag() ? $"{ToString("j")}-{PreReleaseTag.ToString("l")}" : ToString("j");
                case "f":
                    {
                        var buildMetadata = BuildMetaData.ToString();

                        return !string.IsNullOrEmpty(buildMetadata) ? $"{ToString("s")}+{buildMetadata}" : ToString("s");
                    }
                case "i":
                    {
                        var buildMetadata = BuildMetaData.ToString("f");

                        return !string.IsNullOrEmpty(buildMetadata) ? $"{ToString("s")}+{buildMetadata}" : ToString("s");
                    }
                default:
                    throw new ArgumentException($"Unrecognised format '{format}'", nameof(format));
            }
        }

        public SemanticVersion IncrementVersion(VersionField incrementStrategy)
        {
            var incremented = new SemanticVersion(this);
            if (!incremented.PreReleaseTag.HasTag())
            {
                switch (incrementStrategy)
                {
                    case VersionField.None:
                        break;
                    case VersionField.Major:
                        incremented.Major++;
                        incremented.Minor = 0;
                        incremented.Patch = 0;
                        break;
                    case VersionField.Minor:
                        incremented.Minor++;
                        incremented.Patch = 0;
                        break;
                    case VersionField.Patch:
                        incremented.Patch++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                if (incremented.PreReleaseTag.Number != null)
                {
                    incremented.PreReleaseTag.Number = incremented.PreReleaseTag.Number;
                    incremented.PreReleaseTag.Number++;
                }
            }

            return incremented;
        }
    }

    public enum VersionField
    {
        None,
        Patch,
        Minor,
        Major
    }
}
