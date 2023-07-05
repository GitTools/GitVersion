using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public class NextVersion : IComparable<NextVersion>, IEquatable<NextVersion>
{
    public BaseVersion BaseVersion { get; }

    public SemanticVersion IncrementedVersion { get; }

    public IBranch Branch { get; }

    public EffectiveConfiguration Configuration { get; }

    public NextVersion(SemanticVersion incrementedVersion, BaseVersion baseVersion, EffectiveBranchConfiguration configuration)
        : this(incrementedVersion, baseVersion, configuration.NotNull().Branch, configuration.NotNull().Value)
    {
    }

    public NextVersion(SemanticVersion incrementedVersion, BaseVersion baseVersion, IBranch branch, EffectiveConfiguration configuration)
    {
        IncrementedVersion = incrementedVersion.NotNull();
        BaseVersion = baseVersion.NotNull();
        Configuration = configuration.NotNull();
        Branch = branch.NotNull();
    }

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
