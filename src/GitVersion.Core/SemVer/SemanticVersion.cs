using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GitVersion.Extensions;

namespace GitVersion;

/// <summary>Represents a semantic version with optional pre-release tag and build metadata.</summary>
public sealed class SemanticVersion : IFormattable, IComparable<SemanticVersion>, IEquatable<SemanticVersion?>
{
    /// <summary>A zero-valued semantic version with no pre-release tag or build metadata.</summary>
    public static readonly SemanticVersion Empty = new();

    /// <summary>Gets or initializes the major version component.</summary>
    public long Major { get; init; }

    /// <summary>Gets or initializes the minor version component.</summary>
    public long Minor { get; init; }

    /// <summary>Gets or initializes the patch version component.</summary>
    public long Patch { get; init; }

    /// <summary>Gets a value indicating whether this version has a pre-release tag.</summary>
    public bool IsPreRelease => PreReleaseTag.HasTag();

    /// <summary>Gets or initializes the pre-release tag.</summary>
    public SemanticVersionPreReleaseTag PreReleaseTag { get; init; }

    /// <summary>Gets or initializes the build metadata associated with this version.</summary>
    public SemanticVersionBuildMetaData BuildMetaData { get; init; }

    /// <summary>Returns <see langword="true"/> when the pre-release tag name equals <paramref name="value"/> (case-insensitive).</summary>
    public bool IsLabeledWith(string value) => PreReleaseTag.HasTag() && PreReleaseTag.Name.IsEquivalentTo(value);

    /// <summary>Returns <see langword="true"/> when this version is compatible with a branch-specific label check: no tag set, no label supplied, or the label matches.</summary>
    public bool IsMatchForBranchSpecificLabel(string? value)
        => (PreReleaseTag.Name.Length == 0 && PreReleaseTag.Number is null) || value is null || IsLabeledWith(value);

    /// <summary>Initializes a new semantic version with the given major, minor, and patch values.</summary>
    public SemanticVersion(long major = 0, long minor = 0, long patch = 0)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreReleaseTag = SemanticVersionPreReleaseTag.Empty;
        BuildMetaData = SemanticVersionBuildMetaData.Empty;
    }

    /// <summary>Initializes a new semantic version as a copy of <paramref name="semanticVersion"/>.</summary>
    public SemanticVersion(SemanticVersion semanticVersion)
    {
        semanticVersion.NotNull();

        Major = semanticVersion.Major;
        Minor = semanticVersion.Minor;
        Patch = semanticVersion.Patch;

        PreReleaseTag = semanticVersion.PreReleaseTag;
        BuildMetaData = semanticVersion.BuildMetaData;
    }

    /// <summary>Returns <see langword="true"/> when this instance equals <see cref="Empty"/>.</summary>
    public bool IsEmpty() => Equals(Empty);

    /// <summary>Returns <see langword="true"/> when this version is equal to <paramref name="obj"/>.</summary>
    public bool Equals(SemanticVersion? obj)
    {
        if (obj == null)
        {
            return false;
        }
        return Major == obj.Major
            && Minor == obj.Minor
            && Patch == obj.Patch
            && PreReleaseTag == obj.PreReleaseTag
            && BuildMetaData == obj.BuildMetaData;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="obj"/> is a <see cref="SemanticVersion"/> equal to this instance.</summary>
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

    /// <summary>Returns a hash code combining all version components.</summary>
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreReleaseTag, BuildMetaData);

    /// <summary>Returns <see langword="true"/> when <paramref name="v1"/> and <paramref name="v2"/> are equal.</summary>
    public static bool operator ==(SemanticVersion? v1, SemanticVersion? v2)
    {
        if (v1 is null)
        {
            return v2 is null;
        }
        return v1.Equals(v2);
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="v1"/> and <paramref name="v2"/> are not equal.</summary>
    public static bool operator !=(SemanticVersion? v1, SemanticVersion? v2) => !(v1 == v2);

    /// <summary>Returns <see langword="true"/> when <paramref name="v1"/> is greater than <paramref name="v2"/>.</summary>
    public static bool operator >(SemanticVersion v1, SemanticVersion v2)
    {
        ArgumentNullException.ThrowIfNull(v1);
        ArgumentNullException.ThrowIfNull(v2);

        return v1.CompareTo(v2) > 0;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="v1"/> is greater than or equal to <paramref name="v2"/>.</summary>
    public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
    {
        ArgumentNullException.ThrowIfNull(v1);
        ArgumentNullException.ThrowIfNull(v2);

        return v1.CompareTo(v2) >= 0;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="v1"/> is less than or equal to <paramref name="v2"/>.</summary>
    public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
    {
        ArgumentNullException.ThrowIfNull(v1);
        ArgumentNullException.ThrowIfNull(v2);

        return v1.CompareTo(v2) <= 0;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="v1"/> is less than <paramref name="v2"/>.</summary>
    public static bool operator <(SemanticVersion v1, SemanticVersion v2)
    {
        ArgumentNullException.ThrowIfNull(v1);
        ArgumentNullException.ThrowIfNull(v2);

        return v1.CompareTo(v2) < 0;
    }

    /// <summary>Parses <paramref name="version"/> as a semantic version, throwing when the input cannot be parsed.</summary>
    public static SemanticVersion Parse(
        string version, string? tagPrefixRegex, SemanticVersionFormat versionFormat = SemanticVersionFormat.Strict)
    {
        if (!TryParse(version, tagPrefixRegex, out var semanticVersion, versionFormat))
        {
            throw new WarningException($"Failed to parse {version} into a Semantic Version");
        }

        return semanticVersion;
    }

    /// <summary>Attempts to parse <paramref name="version"/> as a semantic version, returning <see langword="true"/> on success.</summary>
    public static bool TryParse(string version, string? tagPrefixRegex,
        [NotNullWhen(true)] out SemanticVersion? semanticVersion, SemanticVersionFormat format = SemanticVersionFormat.Strict)
    {
        var regex = RegexPatterns.Cache.GetOrAdd($"^({tagPrefixRegex})(?<version>.*)$");
        var match = regex.Match(version);

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
        var parsed = RegexPatterns.SemanticVersion.ParseStrictRegex.Match(version);

        if (!parsed.Success)
        {
            semanticVersion = null;
            return false;
        }

        semanticVersion = new()
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
        var parsed = RegexPatterns.SemanticVersion.ParseLooseRegex.Match(version);

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

        semanticVersion = new()
        {
            Major = long.Parse(parsed.Groups["Major"].Value),
            Minor = parsed.Groups["Minor"].Success ? long.Parse(parsed.Groups["Minor"].Value) : 0,
            Patch = parsed.Groups["Patch"].Success ? long.Parse(parsed.Groups["Patch"].Value) : 0,
            PreReleaseTag = SemanticVersionPreReleaseTag.Parse(parsed.Groups["Tag"].Value),
            BuildMetaData = semanticVersionBuildMetaData
        };

        return true;
    }

    /// <summary>Returns <see langword="true"/> when this version is greater than <paramref name="value"/>.</summary>
    public bool IsGreaterThan(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) > 0;

    /// <summary>Returns <see langword="true"/> when this version is greater than or equal to <paramref name="value"/>.</summary>
    public bool IsGreaterThanOrEqualTo(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) >= 0;

    /// <summary>Returns <see langword="true"/> when this version is less than <paramref name="value"/>.</summary>
    public bool IsLessThan(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) < 0;

    /// <summary>Returns <see langword="true"/> when this version is less than or equal to <paramref name="value"/>.</summary>
    public bool IsLessThanOrEqualTo(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) <= 0;

    /// <summary>Returns <see langword="true"/> when this version is equal to <paramref name="value"/>.</summary>
    public bool IsEqualTo(SemanticVersion? value, bool includePreRelease = true)
        => CompareTo(value, includePreRelease) == 0;

    /// <summary>Compares this version to <paramref name="value"/>, including the pre-release tag in the comparison.</summary>
    public int CompareTo(SemanticVersion? value) => CompareTo(value, includePreRelease: true);

    /// <summary>Compares this version to <paramref name="value"/>, optionally excluding the pre-release tag from the comparison.</summary>
    public int CompareTo(SemanticVersion? value, bool includePreRelease)
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

        if (!includePreRelease || PreReleaseTag == value.PreReleaseTag)
        {
            return 0;
        }

        if (PreReleaseTag > value.PreReleaseTag)
        {
            return 1;
        }
        return -1;
    }

    /// <summary>Returns the default semantic version string (without build metadata).</summary>
    public override string ToString() => ToString("s");

    /// <summary>Returns a string representation of this version using the given format specifier.</summary>
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
        {
            format = "s";
        }

        if (formatProvider?.GetFormat(GetType()) is ICustomFormatter formatter)
        {
            return formatter.Format(format, this, formatProvider);
        }

        // Check for lp first because the param can vary
        format = format.ToLower();
        switch (format)
        {
            case "j":
                return $"{Major}.{Minor}.{Patch}";
            case "s":
                return PreReleaseTag.HasTag() ? $"{ToString("j")}-{PreReleaseTag}" : ToString("j");
            case "t":
                return PreReleaseTag.HasTag() ? $"{ToString("j")}-{PreReleaseTag:t}" : ToString("j");
            case "f":
                {
                    var buildMetadata = BuildMetaData.ToString();

                    return !buildMetadata.IsNullOrEmpty() ? $"{ToString("s")}+{buildMetadata}" : ToString("s");
                }
            case "i":
                {
                    var buildMetadata = BuildMetaData.ToString("f");

                    return !buildMetadata.IsNullOrEmpty() ? $"{ToString("s")}+{buildMetadata}" : ToString("s");
                }
            default:
                throw new FormatException($"Unknown format '{format}'.");
        }
    }

    /// <summary>Returns a new version derived from this one with the pre-release tag changed to <paramref name="label"/>.</summary>
    public SemanticVersion WithLabel(string? label) => Increment(VersionField.None, label, mode: IncrementMode.Standard);

    /// <summary>Returns a new version incremented by <paramref name="increment"/> with the given <paramref name="label"/>.</summary>
    public SemanticVersion Increment(
            VersionField increment, string? label, params SemanticVersion?[] alternativeSemanticVersions)
        => Increment(increment, label, mode: IncrementMode.Standard, alternativeSemanticVersions);

    /// <summary>Returns a new version incremented by <paramref name="increment"/> with the given <paramref name="label"/>, optionally forcing the increment.</summary>
    public SemanticVersion Increment(
            VersionField increment, string? label, bool forceIncrement, params SemanticVersion?[] alternativeSemanticVersions)
        => Increment(increment, label, mode: forceIncrement ? IncrementMode.Force : IncrementMode.Standard, alternativeSemanticVersions);

    /// <summary>Returns a new version incremented by <paramref name="increment"/> with the given <paramref name="label"/> and increment <paramref name="mode"/>.</summary>
    public SemanticVersion Increment(
        VersionField increment, string? label, IncrementMode mode, params SemanticVersion?[] alternativeSemanticVersions)
    {
        var major = Major;
        var minor = Minor;
        var patch = Patch;
        var preReleaseNumber = PreReleaseTag.Number;

        var hasPreReleaseTag = PreReleaseTag.HasTag();

        switch (increment)
        {
            case VersionField.None:
                preReleaseNumber++;
                break;

            case VersionField.Patch:
                if (hasPreReleaseTag && (mode == IncrementMode.Standard
                    || (mode == IncrementMode.EnsureIntegrity && patch != 0)))
                {
                    preReleaseNumber++;
                }
                else
                {
                    patch++;
                    if (preReleaseNumber.HasValue)
                    {
                        preReleaseNumber = 1;
                    }
                }
                break;

            case VersionField.Minor:
                if (hasPreReleaseTag && (mode == IncrementMode.Standard
                    || (mode == IncrementMode.EnsureIntegrity && minor != 0 && patch == 0)))
                {
                    preReleaseNumber++;
                }
                else
                {
                    minor++;
                    patch = 0;
                    if (preReleaseNumber.HasValue)
                    {
                        preReleaseNumber = 1;
                    }
                }
                break;

            case VersionField.Major:
                if (hasPreReleaseTag && (mode == IncrementMode.Standard
                    || (mode == IncrementMode.EnsureIntegrity && major != 0 && minor == 0 && patch == 0)))
                {
                    preReleaseNumber++;
                }
                else
                {
                    major++;
                    minor = 0;
                    patch = 0;
                    if (preReleaseNumber.HasValue)
                    {
                        preReleaseNumber = 1;
                    }
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(increment));
        }

        SemanticVersion semanticVersion = new(major, minor, patch);

        var foundAlternativeSemanticVersion = false;
        foreach (var alternativeSemanticVersion in alternativeSemanticVersions)
        {
            if (!semanticVersion.IsLessThan(alternativeSemanticVersion, includePreRelease: false))
            {
                continue;
            }

            semanticVersion = alternativeSemanticVersion!;
            foundAlternativeSemanticVersion = true;
        }

        major = semanticVersion.Major;
        minor = semanticVersion.Minor;
        patch = semanticVersion.Patch;

        if (foundAlternativeSemanticVersion && increment == VersionField.None)
        {
            preReleaseNumber = 1;
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

        if (label is null || preReleaseTagName == label)
        {
            return new SemanticVersion(this) { Major = major, Minor = minor, Patch = patch, PreReleaseTag = new SemanticVersionPreReleaseTag(preReleaseTagName, preReleaseNumber, true) };
        }

        preReleaseNumber = 1;
        preReleaseTagName = label;

        return new SemanticVersion(this)
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            PreReleaseTag = new SemanticVersionPreReleaseTag(preReleaseTagName, preReleaseNumber, true)
        };
    }

    /// <summary>Controls how version incrementing is applied.</summary>
    public enum IncrementMode
    {
        /// <summary>Increments the pre-release number when already on a pre-release, otherwise bumps the field.</summary>
        Standard,

        /// <summary>Always bumps the version field regardless of whether a pre-release tag is present.</summary>
        Force,

        /// <summary>Ensures the resulting version is strictly greater than the current one without forcing an unnecessary bump.</summary>
        EnsureIntegrity
    }
}
