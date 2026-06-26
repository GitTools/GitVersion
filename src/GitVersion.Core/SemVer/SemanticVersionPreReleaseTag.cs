using System.Globalization;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

/// <summary>Represents the pre-release label and optional numeric identifier of a semantic version (e.g. <c>beta.1</c>).</summary>
public sealed class SemanticVersionPreReleaseTag :
    IFormattable, IComparable<SemanticVersionPreReleaseTag>, IEquatable<SemanticVersionPreReleaseTag?>
{
    private static readonly StringComparer IgnoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;

    /// <summary>An empty pre-release tag with no name and no number.</summary>
    public static readonly SemanticVersionPreReleaseTag Empty = new();

    private static readonly LambdaEqualityHelper<SemanticVersionPreReleaseTag> EqualityHelper =
        new(x => x.Name, x => x.Number);

    /// <summary>Gets or initializes the pre-release label (e.g. <c>beta</c>).</summary>
    public string Name { get; init; }

    /// <summary>Gets or initializes the numeric identifier appended after the label (e.g. the <c>1</c> in <c>beta.1</c>).</summary>
    public long? Number { get; init; }

    /// <summary>Gets or initializes a value indicating whether the tag should be promoted even when <see cref="Name"/> is empty.</summary>
    public bool PromoteTagEvenIfNameIsEmpty { get; init; }

    /// <summary>Initializes an empty pre-release tag.</summary>
    public SemanticVersionPreReleaseTag() => Name = string.Empty;

    /// <summary>Initializes a new pre-release tag with the given name, number, and promotion flag.</summary>
    public SemanticVersionPreReleaseTag(string name, long? number, bool promoteTagEvenIfNameIsEmpty)
    {
        Name = name.NotNull();
        Number = number;
        PromoteTagEvenIfNameIsEmpty = promoteTagEvenIfNameIsEmpty;
    }

    /// <summary>Initializes a new pre-release tag as a copy of <paramref name="preReleaseTag"/>.</summary>
    public SemanticVersionPreReleaseTag(SemanticVersionPreReleaseTag preReleaseTag)
    {
        preReleaseTag.NotNull();

        Name = preReleaseTag.Name;
        Number = preReleaseTag.Number;
        PromoteTagEvenIfNameIsEmpty = preReleaseTag.PromoteTagEvenIfNameIsEmpty;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="obj"/> is a <see cref="SemanticVersionPreReleaseTag"/> equal to this instance.</summary>
    public override bool Equals(object? obj) => Equals(obj as SemanticVersionPreReleaseTag);

    /// <summary>Returns <see langword="true"/> when this tag is equal to <paramref name="other"/> by name and number.</summary>
    public bool Equals(SemanticVersionPreReleaseTag? other) => EqualityHelper.Equals(this, other);

    /// <summary>Returns a hash code based on the tag name and number.</summary>
    public override int GetHashCode() => EqualityHelper.GetHashCode(this);

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> are equal.</summary>
    public static bool operator ==(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        Equals(left, right);

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> are not equal.</summary>
    public static bool operator !=(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        !Equals(left, right);

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than <paramref name="right"/>.</summary>
    public static bool operator >(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        left?.CompareTo(right) > 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is less than <paramref name="right"/>.</summary>
    public static bool operator <(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        left?.CompareTo(right) < 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than or equal to <paramref name="right"/>.</summary>
    public static bool operator >=(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        left?.CompareTo(right) >= 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is less than or equal to <paramref name="right"/>.</summary>
    public static bool operator <=(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        IgnoreCaseComparer.Compare(left?.Name, right?.Name) != 1;

    /// <summary>Implicitly converts a pre-release tag to its string representation.</summary>
    public static implicit operator string?(SemanticVersionPreReleaseTag? preReleaseTag) => preReleaseTag?.ToString();

    /// <summary>Implicitly parses a string into a <see cref="SemanticVersionPreReleaseTag"/>.</summary>
    public static implicit operator SemanticVersionPreReleaseTag(string? preReleaseTag) => Parse(preReleaseTag);

    /// <summary>Parses a pre-release tag string, returning <see cref="Empty"/> when the input is null or empty.</summary>
    public static SemanticVersionPreReleaseTag Parse(string? preReleaseTag)
    {
        if (preReleaseTag.IsNullOrEmpty()) return Empty;

        var match = RegexPatterns.SemanticVersion.ParsePreReleaseTagRegex.Match(preReleaseTag);
        if (!match.Success)
        {
            // TODO check how to log this
            Console.WriteLine($"Unable to successfully parse semver tag {preReleaseTag}");
            return Empty;
        }

        var value = match.Groups["name"].Value;
        var number = match.Groups["number"].Success ? long.Parse(match.Groups["number"].Value) : (long?)null;
        return value.EndsWith('-')
            ? new SemanticVersionPreReleaseTag(preReleaseTag, null, true)
            : new SemanticVersionPreReleaseTag(value, number, true);
    }

    /// <summary>Compares this tag to <paramref name="other"/> by name and number.</summary>
    public int CompareTo(SemanticVersionPreReleaseTag? other)
    {
        if (!HasTag() && other?.HasTag() == true)
        {
            return 1;
        }
        if (HasTag() && other?.HasTag() != true)
        {
            return -1;
        }

        var nameComparison = IgnoreCaseComparer.Compare(Name, other?.Name);
        return nameComparison != 0 ? nameComparison : Nullable.Compare(Number, other?.Number);
    }

    /// <summary>Returns the default SemVer 2.0 formatted tag string (e.g. <c>beta.1</c>).</summary>
    public override string ToString() => ToString("t");

    /// <summary>Returns a string representation using the given format specifier.</summary>
    public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// Default formats:
    /// <para>t - SemVer 2.0 formatted tag [beta.1]</para>
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (formatProvider?.GetFormat(GetType()) is ICustomFormatter formatter)
            return formatter.Format(format, this, formatProvider);

        if (format.IsNullOrEmpty())
            format = "t";

        format = format.ToLower();

        return format switch
        {
            "t" => (Number.HasValue ? Name.IsNullOrEmpty() ? $"{Number}" : $"{Name}.{Number}" : Name),
            _ => throw new FormatException($"Unknown format '{format}'.")
        };
    }

    /// <summary>Returns <see langword="true"/> when this tag has a non-empty name or a number with the promotion flag set.</summary>
    public bool HasTag() => !Name.IsNullOrEmpty() || (Number.HasValue && PromoteTagEvenIfNameIsEmpty);
}
