using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public abstract class VersionStrategyBaseWithInheritSupport : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> contextLazy;

    protected GitVersionContext Context => contextLazy.Value;

    protected IRepositoryStore RepositoryStore { get; }

    protected VersionStrategyBaseWithInheritSupport(IRepositoryStore repositoryStore, Lazy<GitVersionContext> contextLazy)
    {
        this.contextLazy = contextLazy.NotNull();
        RepositoryStore = repositoryStore.NotNull();
    }

    IEnumerable<(SemanticVersion IncrementedVersion, BaseVersion Version)> IVersionStrategy.GetVersions()
    {
        foreach (var baseVersion in GetVersionsRecursive(Context.CurrentBranch, null, new()))
        {
            // This comes from BaseVersionCalculator:
            var incrementedVersion = RepositoryStore.MaybeIncrement(baseVersion, Context);
            if (Context.FullConfiguration.VersioningMode == VersioningMode.Mainline)
            {
                if (!(incrementedVersion.PreReleaseTag?.HasTag() != true))
                {
                    continue;
                }
            }

            yield return new(incrementedVersion, baseVersion);
        }
    }

    private IEnumerable<BaseVersion> GetVersionsRecursive(IBranch branch, BranchConfig? childBranchConfiguration, HashSet<IBranch> traversedBranches)
    {
        if (!traversedBranches.Add(branch)) yield break;

        var branchConfiguration = Context.FullConfiguration.GetBranchConfiguration(branch.Name.WithoutRemote);
        if (childBranchConfiguration != null)
        {
            branchConfiguration = childBranchConfiguration.Inherit(branchConfiguration);
        }

        var branches = Array.Empty<IBranch>();
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            branches = RepositoryStore.GetTargetBranches(branch, Context.FullConfiguration, traversedBranches).ToArray();

            if (branches.Length == 0)
            {
                var fallbackBranchConfiguration = Context.FullConfiguration.GetFallbackBranchConfiguration();
                if (fallbackBranchConfiguration.Increment == IncrementStrategy.Inherit)
                {
                    fallbackBranchConfiguration.Increment = IncrementStrategy.None;
                }
                branchConfiguration = branchConfiguration.Inherit(fallbackBranchConfiguration);
            }
        }

        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            foreach (var item in branches)
            {
                if (Context.CurrentBranch == item) continue;
                foreach (var baseVersion in GetVersionsRecursive(item, branchConfiguration, traversedBranches))
                {
                    yield return baseVersion;
                }
            }
        }
        else
        {
            var effectiveConfiguration = new EffectiveConfiguration(Context.FullConfiguration, branchConfiguration);
            Context.Configuration = effectiveConfiguration;
            foreach (var baseVersion in GetVersions(branch, effectiveConfiguration))
            {
                yield return baseVersion;
            }
        }
    }

    public abstract IEnumerable<BaseVersion> GetVersions(IBranch branch, EffectiveConfiguration configuration);
}
