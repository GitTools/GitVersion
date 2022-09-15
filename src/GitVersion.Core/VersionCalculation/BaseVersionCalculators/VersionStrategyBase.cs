using GitVersion.Common;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public abstract class VersionStrategyBase : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> versionContext;

    protected GitVersionContext Context => this.versionContext.Value;

    protected IRepositoryStore RepositoryStore { get; }

    protected VersionStrategyBase(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    {
        this.versionContext = versionContext.NotNull();
        RepositoryStore = repositoryStore.NotNull();
    }

    IEnumerable<(SemanticVersion IncrementedVersion, BaseVersion Version)> IVersionStrategy.GetVersions()
    {
        foreach (var baseVersion in GetVersions())
        {
            var incrementedVersion = RepositoryStore.MaybeIncrement(baseVersion, Context);
            if (Context.Configuration!.VersioningMode == VersioningMode.Mainline)
            {
                if (!(incrementedVersion.PreReleaseTag?.HasTag() != true))
                {
                    continue;
                }
            }

            yield return new(incrementedVersion, baseVersion);
        }
    }

    public abstract IEnumerable<BaseVersion> GetVersions();
}
