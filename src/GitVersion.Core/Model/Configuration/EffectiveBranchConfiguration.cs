using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Model.Configuration;

public class EffectiveBranchConfiguration
{
    public IBranch Branch { get; }

    public EffectiveConfiguration Configuration { get; }

    public EffectiveBranchConfiguration(IBranch branch, EffectiveConfiguration configuration)
    {
        Branch = branch.NotNull();
        Configuration = configuration.NotNull();
    }

    public NextVersion CreateNextVersion(BaseVersion baseVersion, SemanticVersion incrementedVersion)
        => new(incrementedVersion.NotNull(), baseVersion.NotNull(), new(Branch, Configuration));
}
