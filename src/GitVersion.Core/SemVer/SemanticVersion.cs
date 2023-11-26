using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Extensions;

namespace GitVersion;

public class SemanticVersion : IFormattable, IComparable<SemanticVersion>, IEquatable<SemanticVersion?>
{
    public static readonly SemanticVersion Empty = new();

    // uses the git-semver spec https://github.com/semver/semver/blob/master/semver.md
    private static readonly Regex ParseSemVerStrict = new(
        @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ParseSemVerLoose = new(
        @"^(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public long Major { get; init; }

    public long Minor { get; init; }

    public long Patch { get; init; }

    public bool IsPreRelease => PreReleaseTag.HasTag();

    public SemanticVersionPreReleaseTag PreReleaseTag { get; init; }

    public SemanticVersionBuildMetaData BuildMetaData { get; init; }

    public bool IsLabeledWith(string value) => PreReleaseTag.HasTag() && PreReleaseTag.Name.IsEquivalentTo(value);

    public bool IsMatchForBranchSpecificLabel(string? value)
        => (PreReleaseTag.Name == string.Empty && PreReleaseTag.Number is null) || value is null || IsLabeledWith(value);

    public SemanticVersion(long major = 0, long minor = 0, long patch = 0)
    {
        this.Major = major;
        this.Minor = minor;
        this.Patch = patch;
        this.PreReleaseTag = SemanticVersionPreReleaseTag.Empty;
        this.BuildMetaData = SemanticVersionBuildMetaData.Empty;
    }

    public SemanticVersion(SemanticVersion semanticVersion)
    {
        semanticVersion.NotNull();

        this.Major = semanticVersion.Major;
        this.Minor = semanticVersion.Minor;
        this.Patch = semanticVersion.Patch;

        this.PreReleaseTag = semanticVersion.PreReleaseTag;
        this.BuildMetaData = semanticVersion.BuildMetaData;
    }

    public bool Equals(SemanticVersion? obj)
    {
        if (obj == null)
        {
            return false;
        }
        return this.Major == obj.Major && this.Minor == obj.Minor && this.Patch == obj.Patch && this.PreReleaseTag == obj.PreReleaseTag && this.BuildMetaData == obj.BuildMetaData;
    }

    public bool IsEmpty() => Equals(Empty);

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        return obj.GetType() == GetType() && Equals((SemanticVersion)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Major.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Minor.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Patch.GetHashCode();
            hashCode = (hashCode * 397) ^ this.PreReleaseTag.GetHashCode();
            hashCode = (hashCode * 397) ^ this.BuildMetaData.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(SemanticVersion? v1, SemanticVersion? v2)
    {
        if (v1 is null)
        {
            return v2 is null;
        }
        return v1.Equals(v2);
    }

    public static bool operator !=(SemanticVersion? v1, SemanticVersion? v2) => !(v1 == v2);

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

    public static SemanticVersion Parse(string version, string? tagPrefixRegex, SemanticVersionFormat versionFormat = SemanticVersionFormat.Strict)
    {
        if (!TryParse(version, tagPrefixRegex, out var semanticVersion, versionFormat))
            throw new WarningException($"Failed to parse {version} into a Semantic Version");

        return semanticVersion;
    }

    public static bool TryParse(string version, string? tagPrefixRegex, [NotNullWhen(true)] out SemanticVersion? semanticVersion, SemanticVersionFormat format = SemanticVersionFormat.Strict)
    {
        var match = Regex.Match(version, $"^({tagPrefixRegex})(?<version>.*)$");

        if (!match.Success)
        {
            semanticVersion = null;
            return false;
        }

        version = match.Groups["version"].Value;
        return format == SemanticVersionFormat.Strict
            ? TryParseStrict(version, out semanticVersion)
            : TryParseLoose(version, out semanticVersion);
    }

    private static bool TryParseStrict(string version, [NotNullWhen(true)] out SemanticVersion? semanticVersion)
    {
        var parsed = ParseSemVerStrict.Match(version);

        if (!parsed.Success)
        {
            semanticVersion = null;
            return false;
        }

        semanticVersion = new SemanticVersion
        {
            Major = long.Parse(parsed.Groups["major"].Value),
            Minor = parsed.Groups["minor"].Success ? long.Parse(parsed.Groups["minor"].Value) : 0,
            Patch = parsed.Groups["patch"].Success ? long.Parse(parsed.Groups["patch"].Value) : 0,
            PreReleaseTag = SemanticVersionPreReleaseTag.Parse(parsed.Groups["prerelease"].Value),
            BuildMetaData = SemanticVersionBuildMetaData.Parse(parsed.Groups["buildmetadata"].Value)
        };

        return true;
    }

    private static bool TryParseLoose(string version, [NotNullWhen(true)] out SemanticVersion? semanticVersion)
    {
        var parsed = ParseSemVerLoose.Match(version);

        if (!parsed.Success)
        {
            semanticVersion = null;
            return false;
        }

        var semanticVersionBuildMetaData = SemanticVersionBuildMetaData.Parse(parsed.Groups["BuildMetaData"].Value);
        var fourthPart = parsed.Groups["FourthPart"];
        if (fourthPart.Success && semanticVersionBuildMetaData.CommitsSinceTag == null)
        {
            semanticVersionBuildMetaData = new(semanticVersionBuildMetaData)
            {
                CommitsSinceTag = int.Parse(fourthPart.Value)
            };
        }

        semanticVersion = new SemanticVersion
        {
            Major = long.Parse(parsed.Groups["Major"].Value),
            Minor = parsed.Groups["Minor"].Success ? long.Parse(parsed.Groups["Minor"].Value) : 0,
            Patch = parsed.Groups["Patch"].Success ? long.Parse(parsed.Groups["Patch"].Value) : 0,
            PreReleaseTag = SemanticVersionPreReleaseTag.Parse(parsed.Groups["Tag"].Value),
            BuildMetaData = semanticVersionBuildMetaData
        };

        return true;
    }

    public bool IsGreaterThan(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) > 0;

    public bool IsGreaterThanOrEqualTo(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) >= 0;

    public bool IsLessThan(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) < 0;

    public bool IsLessThanOrEqualTo(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) <= 0;

    public bool IsEqualTo(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) == 0;

    public int CompareTo(SemanticVersion? value) => CompareTo(value, includePreRelease: true);

    public int CompareTo(SemanticVersion? value, bool includePreRelease)
    {
        if (value == null)
        {
            return 1;
        }
        if (this.Major != value.Major)
        {
            if (this.Major > value.Major)
            {
                return 1;
            }
            return -1;
        }
        if (this.Minor != value.Minor)
        {
            if (this.Minor > value.Minor)
            {
                return 1;
            }
            return -1;
        }
        if (this.Patch != value.Patch)
        {
            if (this.Patch > value.Patch)
            {
                return 1;
            }
            return -1;
        }
        if (includePreRelease && this.PreReleaseTag != value.PreReleaseTag)
        {
            if (this.PreReleaseTag > value.PreReleaseTag)
            {
                return 1;
            }
            return -1;
        }

        return 0;
    }

    public override string ToString() => ToString("s");

    public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// <para>s - Default SemVer [1.2.3-beta.4]</para>
    /// <para>f - Full SemVer [1.2.3-beta.4+5]</para>
    /// <para>i - Informational SemVer [1.2.3-beta.4+5.Branch.main.BranchType.main.Sha.000000]</para>
    /// <para>j - Just the SemVer part [1.2.3]</para>
    /// <para>t - SemVer with the tag [1.2.3-beta.4]</para>
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (format.IsNullOrEmpty())
            format = "s";

        if (formatProvider?.GetFormat(GetType()) is ICustomFormatter formatter)
            return formatter.Format(format, this, formatProvider);

        // Check for lp first because the param can vary
        format = format.ToLower();
        switch (format)
        {
            case "j":
                return $"{this.Major}.{this.Minor}.{this.Patch}";
            case "s":
                return this.PreReleaseTag.HasTag() ? $"{ToString("j")}-{this.PreReleaseTag}" : ToString("j");
            case "t":
                return this.PreReleaseTag.HasTag() ? $"{ToString("j")}-{this.PreReleaseTag:t}" : ToString("j");
            case "f":
                {
                    var buildMetadata = this.BuildMetaData.ToString();

                    return !buildMetadata.IsNullOrEmpty() ? $"{ToString("s")}+{buildMetadata}" : ToString("s");
                }
            case "i":
                {
                    var buildMetadata = this.BuildMetaData.ToString("f");

                    return !buildMetadata.IsNullOrEmpty() ? $"{ToString("s")}+{buildMetadata}" : ToString("s");
                }
            default:
                throw new FormatException($"Unknown format '{format}'.");
        }
    }

    public SemanticVersion IncrementVersion(VersionField incrementStrategy)
        => IncrementVersion(incrementStrategy, null, isMainRelease: true);

    private SemanticVersion IncrementVersion(VersionField incrementStrategy, string? label, bool isMainRelease)
    {
        var major = Major;
        var minor = Minor;
        var patch = Patch;

        if (isMainRelease || !PreReleaseTag.HasTag())
        {
            switch (incrementStrategy)
            {
                case VersionField.None:
                    break;
                case VersionField.Major:
                    major++;
                    minor = 0;
                    patch = 0;
                    break;
                case VersionField.Minor:
                    minor++;
                    patch = 0;
                    break;
                case VersionField.Patch:
                    patch++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(incrementStrategy));
            }
        }

        string preReleaseTagName = string.Empty;
        long? preReleaseTagNumber = null;

        if (!isMainRelease)
        {
            if (PreReleaseTag.HasTag())
            {
                preReleaseTagNumber = PreReleaseTag.Number + 1;
                preReleaseTagName = PreReleaseTag.Name;
            }
            else
            {
                preReleaseTagNumber = 1;
                preReleaseTagName = label ?? string.Empty;
            }
        }

        return new SemanticVersion(this)
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            PreReleaseTag = new SemanticVersionPreReleaseTag(preReleaseTagName, preReleaseTagNumber, true)
        };
    }

    public SemanticVersion Increment(VersionField incrementStrategy, string? label)
        => Increment(incrementStrategy, label, mode: IncrementMode.Standard);

    public SemanticVersion Increment(VersionField incrementStrategy, string? label, bool forceIncrement)
        => Increment(incrementStrategy, label, mode: forceIncrement ? IncrementMode.Force : IncrementMode.Standard);

    public SemanticVersion Increment(VersionField increment, string? label, IncrementMode mode)
    {
        long major = Major;
        long minor = Minor;
        long patch = Patch;
        long? preReleaseNumber = PreReleaseTag.Number;

        bool hasPreReleaseTag = PreReleaseTag.HasTag();

        switch (increment)
        {
            case VersionField.None:
                preReleaseNumber++;
                break;

            case VersionField.Patch:
                if (hasPreReleaseTag && (mode == IncrementMode.Standard
                    || mode == IncrementMode.EnsureIntegrity && patch != 0))
                {
                    preReleaseNumber++;
                }
                else
                {
                    patch++;
                    if (preReleaseNumber.HasValue) preReleaseNumber = 1;
                }
                break;

            case VersionField.Minor:
                if (hasPreReleaseTag && (mode == IncrementMode.Standard
                    || mode == IncrementMode.EnsureIntegrity && minor != 0 && patch == 0))
                {
                    preReleaseNumber++;
                }
                else
                {
                    minor++;
                    patch = 0;
                    if (preReleaseNumber.HasValue) preReleaseNumber = 1;
                }
                break;

            case VersionField.Major:
                if (hasPreReleaseTag && (mode == IncrementMode.Standard
                    || mode == IncrementMode.EnsureIntegrity && major != 0 && minor == 0 && patch == 0))
                {
                    preReleaseNumber++;
                }
                else
                {
                    major++;
                    minor = 0;
                    patch = 0;
                    if (preReleaseNumber.HasValue) preReleaseNumber = 1;
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(increment));
        }

        string preReleaseTagName;
        if (hasPreReleaseTag)
        {
            preReleaseTagName = PreReleaseTag.Name;
        }
        else
        {
            preReleaseNumber = 1;
            preReleaseTagName = string.Empty;
        }

        if (label is not null && preReleaseTagName != label)
        {
            preReleaseNumber = 1;
            preReleaseTagName = label;
        }

        return new SemanticVersion(this)
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            PreReleaseTag = new SemanticVersionPreReleaseTag(preReleaseTagName, preReleaseNumber, true)
        };
    }

    public enum IncrementMode
    {
        Standard,

        Force,

        EnsureIntegrity
    }
}
