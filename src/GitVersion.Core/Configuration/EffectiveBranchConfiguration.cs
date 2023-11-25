using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public record EffectiveBranchConfiguration(EffectiveConfiguration Value, IBranch Branch)
{
    public IBranch Branch { get; } = Branch.NotNull();

    public EffectiveConfiguration Value { get; } = Value.NotNull();

    public NextVersion CreateNextVersion(BaseVersion baseVersion, SemanticVersion incrementedVersion)
    {
        incrementedVersion.NotNull();
        baseVersion.NotNull();

        return new NextVersion(incrementedVersion, baseVersion, this);
    }
}
