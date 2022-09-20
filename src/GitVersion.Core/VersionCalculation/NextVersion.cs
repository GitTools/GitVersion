using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public class NextVersion
{
    public BaseVersion BaseVersion
    {
        get;
        [Obsolete("This NextVersion class needs to be immutable.")]
        set;
    }

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
#pragma warning disable CS0618 // Type or member is obsolete
        BaseVersion = baseVersion.NotNull();
#pragma warning restore CS0618 // Type or member is obsolete
        Configuration = configuration.NotNull();
        Branch = branch.NotNull();
    }

    public override string ToString() => $"{BaseVersion} | {IncrementedVersion}";
}
