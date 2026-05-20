using System.Diagnostics.CodeAnalysis;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal sealed class TrackReleaseBranchesVersionStrategy(
    Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore,
    IBranchRepository branchRepository,
    IIncrementStrategyFinder incrementStrategyFinder,
    IEnvironment environment)
    : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IBranchRepository branchRepository = branchRepository.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();
    private readonly IEnvironment environment = environment.NotNull();
    private readonly VersionInBranchNameVersionStrategy releaseVersionStrategy = new(contextLazy, environment);

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.TrackReleaseBranches))
            yield break;

        if (!configuration.Value.TracksReleaseBranches) yield break;
        foreach (var releaseBranch in this.branchRepository.GetReleaseBranches(Context.Configuration))
        {
            if (TryGetBaseVersion(releaseBranch, configuration, out var baseVersion))
            {
                yield return baseVersion;
            }
        }
    }

    private bool TryGetBaseVersion(
        IBranch releaseBranch, EffectiveBranchConfiguration configuration, [NotNullWhen(true)] out BaseVersion? result)
    {
        result = null;

        var releaseBranchConfiguration = Context.Configuration.GetEffectiveBranchConfiguration(releaseBranch);
        if (!this.releaseVersionStrategy.TryGetBaseVersion(releaseBranchConfiguration, out var baseVersion)) return result is not null;
        // Find the commit where the child branch was created.
        var baseVersionSource = this.repositoryStore.FindMergeBase(releaseBranch, Context.CurrentBranch);
        var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null, this.environment);
        var increment = this.incrementStrategyFinder.DetermineIncrementedField(
            currentCommit: Context.CurrentCommit,
            baseVersionSource: baseVersionSource,
            shouldIncrement: true,
            configuration: configuration.Value,
            label: label
        );

        result = new BaseVersion(
            "Release branch exists -> " + baseVersion.Source, baseVersion.SemanticVersion, baseVersionSource)
        {
            Operator = new BaseVersionOperator
            {
                Increment = increment,
                ForceIncrement = false,
                Label = label
            }
        };

        return true;
    }
}
