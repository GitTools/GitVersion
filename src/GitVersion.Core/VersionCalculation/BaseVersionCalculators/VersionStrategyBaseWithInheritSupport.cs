using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public abstract class VersionStrategyBaseWithInheritSupport : VersionStrategyBase
{
    protected IRepositoryStore RepositoryStore { get; }

    protected VersionStrategyBaseWithInheritSupport(IRepositoryStore repositoryStore, Lazy<GitVersionContext> context)
        : base(context) => RepositoryStore = repositoryStore.NotNull();

    public override IEnumerable<BaseVersion> GetVersions()
    {
        foreach (BaseVersion baseVersion in GetVersionsRecursive(Context.CurrentBranch, new()))
        {
            yield return baseVersion;
        }
    }

    private IEnumerable<BaseVersion> GetVersionsRecursive(IBranch branch, HashSet<IBranch> traversedBranches)
    {
        EffectiveConfiguration configuration = Context.GetEffectiveConfiguration(branch);
        if (configuration.Increment != IncrementStrategy.Inherit)
        {
            foreach (var baseVersion in GetVersions(branch, configuration))
            {
                yield return baseVersion;
            }
        }
        else
        {
            foreach (var branchCommit in RepositoryStore.FindCommitBranchesWasBranchedFrom(branch, Context.FullConfiguration))
            {
                if (!traversedBranches.Add(branchCommit.Branch)) continue;
                foreach (var baseVersion in GetVersionsRecursive(branchCommit.Branch, traversedBranches))
                {
                    yield return baseVersion;
                }
            }
        }
    }

    public abstract IEnumerable<BaseVersion> GetVersions(IBranch branch, EffectiveConfiguration configuration);
}
