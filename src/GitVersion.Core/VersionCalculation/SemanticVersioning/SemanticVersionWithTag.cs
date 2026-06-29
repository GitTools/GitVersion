using GitVersion.Git;

namespace GitVersion;

/// <summary>Pairs a parsed <see cref="SemanticVersion"/> with the <see cref="ITag"/> it was extracted from.</summary>
public sealed record SemanticVersionWithTag(SemanticVersion Value, ITag Tag) : IComparable<SemanticVersionWithTag>
{
    /// <summary>Compares this instance to <paramref name="other"/> by semantic version.</summary>
    public int CompareTo(SemanticVersionWithTag? other) => Value.CompareTo(other?.Value);

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</summary>
    public static bool operator <(SemanticVersionWithTag? left, SemanticVersionWithTag? right) => Comparer<SemanticVersionWithTag>.Default.Compare(left, right) < 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> sorts before or equal to <paramref name="right"/>.</summary>
    public static bool operator <=(SemanticVersionWithTag? left, SemanticVersionWithTag? right) => Comparer<SemanticVersionWithTag>.Default.Compare(left, right) <= 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</summary>
    public static bool operator >(SemanticVersionWithTag? left, SemanticVersionWithTag? right) => Comparer<SemanticVersionWithTag>.Default.Compare(left, right) > 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> sorts after or equal to <paramref name="right"/>.</summary>
    public static bool operator >=(SemanticVersionWithTag? left, SemanticVersionWithTag? right) => Comparer<SemanticVersionWithTag>.Default.Compare(left, right) >= 0;

    /// <summary>Returns a human-readable representation showing the tag, its commit, and the parsed version.</summary>
    public override string ToString() => $"{Tag} | {Tag.Commit} | {Value}";
}
