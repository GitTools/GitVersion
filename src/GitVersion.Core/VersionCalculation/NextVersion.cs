using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public class NextVersion(
    SemanticVersion incrementedVersion,
    IBaseVersion baseVersion,
    VersionField increment,
    EffectiveBranchConfiguration configuration)
    : IComparable<NextVersion>, IEquatable<NextVersion>
{
    public IBaseVersion BaseVersion { get; } = baseVersion.NotNull();

    public SemanticVersion IncrementedVersion { get; } = incrementedVersion.NotNull();

    public EffectiveBranchConfiguration BranchConfiguration { get; } = configuration;

    public VersionField Increment { get; } = increment;

    public EffectiveConfiguration Configuration => BranchConfiguration.Value;

    public int CompareTo(NextVersion? other) => IncrementedVersion.CompareTo(other?.IncrementedVersion);

    public static bool operator ==(NextVersion left, NextVersion? right) => left.CompareTo(right) == 0;

    public static bool operator !=(NextVersion left, NextVersion right) => left.CompareTo(right) != 0;

    public static bool operator <(NextVersion left, NextVersion right) => left.CompareTo(right) < 0;

    public static bool operator <=(NextVersion left, NextVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >(NextVersion left, NextVersion right) => left.CompareTo(right) > 0;

    public static bool operator >=(NextVersion left, NextVersion right) => left.CompareTo(right) >= 0;

    public bool Equals(NextVersion? other) => this == other;

    public override bool Equals(object? other) => other is NextVersion nextVersion && Equals(nextVersion);

    public override string ToString() => $"{BaseVersion} | {IncrementedVersion}";

    public override int GetHashCode() => ToString().GetHashCode();
}
