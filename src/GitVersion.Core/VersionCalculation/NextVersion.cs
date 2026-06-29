using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>Represents the next calculated version together with its base version and branch configuration.</summary>
public sealed class NextVersion(
    SemanticVersion incrementedVersion,
    IBaseVersion baseVersion,
    EffectiveBranchConfiguration configuration)
    : IComparable<NextVersion>, IEquatable<NextVersion>
{
    /// <summary>Gets the base version that was used as the starting point for this calculation.</summary>
    public IBaseVersion BaseVersion { get; } = baseVersion.NotNull();

    /// <summary>Gets the semantic version after applying the required increments.</summary>
    public SemanticVersion IncrementedVersion { get; } = incrementedVersion.NotNull();

    /// <summary>Gets the effective branch configuration under which this version was calculated.</summary>
    public EffectiveBranchConfiguration BranchConfiguration { get; } = configuration;

    /// <summary>Gets the effective configuration values for the branch.</summary>
    public EffectiveConfiguration Configuration => BranchConfiguration.Value;

    /// <summary>Compares this version to <paramref name="other"/> by incremented version.</summary>
    public int CompareTo(NextVersion? other) => IncrementedVersion.CompareTo(other?.IncrementedVersion);

    /// <summary>Returns <see langword="true"/> when the incremented versions of <paramref name="left"/> and <paramref name="right"/> are equal.</summary>
    public static bool operator ==(NextVersion left, NextVersion? right) => left.CompareTo(right) == 0;

    /// <summary>Returns <see langword="true"/> when the incremented versions of <paramref name="left"/> and <paramref name="right"/> are not equal.</summary>
    public static bool operator !=(NextVersion left, NextVersion right) => left.CompareTo(right) != 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is less than <paramref name="right"/>.</summary>
    public static bool operator <(NextVersion left, NextVersion right) => left.CompareTo(right) < 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is less than or equal to <paramref name="right"/>.</summary>
    public static bool operator <=(NextVersion left, NextVersion right) => left.CompareTo(right) <= 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than <paramref name="right"/>.</summary>
    public static bool operator >(NextVersion left, NextVersion right) => left.CompareTo(right) > 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than or equal to <paramref name="right"/>.</summary>
    public static bool operator >=(NextVersion left, NextVersion right) => left.CompareTo(right) >= 0;

    /// <summary>Returns <see langword="true"/> when this instance equals <paramref name="other"/>.</summary>
    public bool Equals(NextVersion? other) => this == other;

    /// <summary>Returns <see langword="true"/> when <paramref name="obj"/> is a <see cref="NextVersion"/> equal to this instance.</summary>
    public override bool Equals(object? obj) => obj is NextVersion nextVersion && Equals(nextVersion);

    /// <summary>Returns a human-readable representation showing the base version and incremented version.</summary>
    public override string ToString() => $"{BaseVersion} | {IncrementedVersion}";

    /// <summary>Returns a hash code based on the string representation.</summary>
    public override int GetHashCode() => ToString().GetHashCode();
}
