using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

public sealed class SemanticVersionPreReleaseTag :
    IFormattable, IComparable<SemanticVersionPreReleaseTag>, IEquatable<SemanticVersionPreReleaseTag?>
{
    private static readonly StringComparer IgnoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
    public static readonly SemanticVersionPreReleaseTag Empty = new();

    private static readonly Regex ParseRegex = new(
        @"(?<name>.*?)\.?(?<number>\d+)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly LambdaEqualityHelper<SemanticVersionPreReleaseTag> EqualityHelper =
        new(x => x.Name, x => x.Number);

    public string Name { get; init; }

    public long? Number { get; init; }

    public bool PromoteTagEvenIfNameIsEmpty { get; init; }

    public SemanticVersionPreReleaseTag() => Name = string.Empty;

    public SemanticVersionPreReleaseTag(string name, long? number, bool promoteTagEvenIfNameIsEmpty)
    {
        Name = name.NotNull();
        Number = number;
        PromoteTagEvenIfNameIsEmpty = promoteTagEvenIfNameIsEmpty;
    }

    public SemanticVersionPreReleaseTag(SemanticVersionPreReleaseTag preReleaseTag)
    {
        preReleaseTag.NotNull();

        Name = preReleaseTag.Name;
        Number = preReleaseTag.Number;
        PromoteTagEvenIfNameIsEmpty = preReleaseTag.PromoteTagEvenIfNameIsEmpty;
    }

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
        IgnoreCaseComparer.Compare(left?.Name, right?.Name) != 1;

    public static implicit operator string?(SemanticVersionPreReleaseTag? preReleaseTag) => preReleaseTag?.ToString();

    public static implicit operator SemanticVersionPreReleaseTag(string? preReleaseTag) => Parse(preReleaseTag);

    public static SemanticVersionPreReleaseTag Parse(string? preReleaseTag)
    {
        if (preReleaseTag.IsNullOrEmpty()) return Empty;

        var match = ParseRegex.Match(preReleaseTag);
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
            "t" => (Number.HasValue ? Name.IsNullOrEmpty() ? $"{Number}" : $"{Name}.{Number}" : Name),
            _ => throw new FormatException($"Unknown format '{format}'.")
        };
    }

    public bool HasTag() => !Name.IsNullOrEmpty() || Number.HasValue && PromoteTagEvenIfNameIsEmpty;
}
