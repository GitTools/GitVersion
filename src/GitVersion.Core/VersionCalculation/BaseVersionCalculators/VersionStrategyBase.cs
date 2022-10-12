using GitVersion.Extensions;
using GitVersion.Model.Configurations;

namespace GitVersion.VersionCalculation;

public abstract class VersionStrategyBase : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> versionContext;

    protected GitVersionContext Context => this.versionContext.Value;

    protected VersionStrategyBase(Lazy<GitVersionContext> versionContext) => this.versionContext = versionContext.NotNull();

    public abstract IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration);
}
