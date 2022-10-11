using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

public class SemanticVersionPreReleaseTag :
    IFormattable, IComparable<SemanticVersionPreReleaseTag>, IEquatable<SemanticVersionPreReleaseTag?>
{
    private static readonly Regex ParseRegex = new(
        @"(?<name>.*?)\.?(?<number>\d+)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly LambdaEqualityHelper<SemanticVersionPreReleaseTag> EqualityHelper =
        new(x => x.Name, x => x.Number);

    public SemanticVersionPreReleaseTag()
    {
    }

    public SemanticVersionPreReleaseTag(string? name, long? number)
    {
        Name = name;
        Number = number;
    }

    public SemanticVersionPreReleaseTag(SemanticVersionPreReleaseTag? preReleaseTag)
    {
        Name = preReleaseTag?.Name;
        Number = preReleaseTag?.Number;
        PromotedFromCommits = preReleaseTag?.PromotedFromCommits;
    }

    public string? Name { get; set; }

    public long? Number { get; set; }

    public bool? PromotedFromCommits { get; set; }

    public override bool Equals(object? obj) => Equals(obj as SemanticVersionPreReleaseTag);

    public bool Equals(SemanticVersionPreReleaseTag? other) => EqualityHelper.Equals(this, other);

    public override int GetHashCode() => EqualityHelper.GetHashCode(this);

    public static bool operator ==(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        Equals(left, right);

    public static bool operator !=(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        !Equals(left, right);

    public static bool operator >(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        left?.CompareTo(right) > 0;

    public static bool operator <(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        left?.CompareTo(right) < 0;

    public static bool operator >=(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        left?.CompareTo(right) >= 0;

    public static bool operator <=(SemanticVersionPreReleaseTag? left, SemanticVersionPreReleaseTag? right) =>
        StringComparerUtils.IgnoreCaseComparer.Compare(left?.Name, right?.Name) != 1;

    public static implicit operator string?(SemanticVersionPreReleaseTag? preReleaseTag) => preReleaseTag?.ToString();

    public static implicit operator SemanticVersionPreReleaseTag(string? preReleaseTag) => Parse(preReleaseTag);

    public static SemanticVersionPreReleaseTag Parse(string? preReleaseTag)
    {
        if (preReleaseTag.IsNullOrEmpty())
        {
            return new SemanticVersionPreReleaseTag();
        }

        var match = ParseRegex.Match(preReleaseTag);
        if (!match.Success)
        {
            // TODO check how to log this
            Console.WriteLine($"Unable to successfully parse semver tag {preReleaseTag}");
            return new SemanticVersionPreReleaseTag();
        }

        var value = match.Groups["name"].Value;
        var number = match.Groups["number"].Success ? long.Parse(match.Groups["number"].Value) : (long?)null;
        return value.EndsWith("-")
            ? new SemanticVersionPreReleaseTag(preReleaseTag, null)
            : new SemanticVersionPreReleaseTag(value, number);
    }

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

        var nameComparison = StringComparerUtils.IgnoreCaseComparer.Compare(Name, other?.Name);
        return nameComparison != 0 ? nameComparison : Nullable.Compare(Number, other?.Number);
    }

    public override string ToString() => ToString("t");

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
            "t" => (Number.HasValue ? Name.IsNullOrEmpty() ? $"{Number}" : $"{Name}.{Number}" : Name ?? string.Empty),
            _ => throw new FormatException($"Unknown format '{format}'.")
        };
    }

    public bool HasTag() =>
        !Name.IsNullOrEmpty() || (Number.HasValue && PromotedFromCommits != true);
}
