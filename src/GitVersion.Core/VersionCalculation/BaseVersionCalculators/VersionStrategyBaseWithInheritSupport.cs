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
        foreach (var item in GetVersionsRecursive(Context.CurrentBranch, null, new()))
        {
            ////
            // Has been moved from BaseVersionCalculator because the effected configuration is only available in this class.
            Context.Configuration = item.Configuration;
            var incrementedVersion = RepositoryStore.MaybeIncrement(item.Version, Context);
            //

            if (Context.FullConfiguration.VersioningMode == VersioningMode.Mainline)
            {
                if (!(incrementedVersion.PreReleaseTag?.HasTag() != true))
                {
                    continue;
                }
            }

            yield return new(incrementedVersion, item.Version);
        }
    }

    private IEnumerable<(EffectiveConfiguration Configuration, BaseVersion Version)> GetVersionsRecursive(IBranch currentBranch,
        BranchConfig? childBranchConfiguration, HashSet<IBranch> traversedBranches)
    {
        if (!traversedBranches.Add(currentBranch)) yield break;

        var branchConfiguration = Context.FullConfiguration.GetBranchConfiguration(currentBranch.Name.WithoutRemote);
        if (childBranchConfiguration != null)
        {
            branchConfiguration = childBranchConfiguration.Inherit(branchConfiguration);
        }

        var branches = Array.Empty<IBranch>();
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            branches = RepositoryStore.GetTargetBranches(currentBranch, Context.FullConfiguration, traversedBranches).ToArray();

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
            foreach (var branch in branches)
            {
                foreach (var item in GetVersionsRecursive(branch, branchConfiguration, traversedBranches))
                {
                    yield return item;
                }
            }
        }
        else
        {
            var effectiveConfiguration = new EffectiveConfiguration(Context.FullConfiguration, branchConfiguration);
            foreach (var baseVersion in GetVersions(currentBranch, effectiveConfiguration))
            {
                yield return new(effectiveConfiguration, baseVersion);
            }
        }
    }

    public abstract IEnumerable<BaseVersion> GetVersions(IBranch branch, EffectiveConfiguration configuration);
}
