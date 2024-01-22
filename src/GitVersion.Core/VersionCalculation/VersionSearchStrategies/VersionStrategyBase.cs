using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public abstract class VersionStrategyBase(Lazy<GitVersionContext> versionContext) : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();

    protected GitVersionContext Context => this.versionContext.Value;

    public abstract IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration);
}
