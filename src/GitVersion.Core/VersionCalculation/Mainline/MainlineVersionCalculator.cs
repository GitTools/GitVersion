using GitVersion.Extensions;
using GitVersion.VersionCalculation.Mainline.NonTrunk;
using GitVersion.VersionCalculation.Mainline.Trunk;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class MainlineVersionCalculator
{
    private readonly MainlineHistory history;

    internal MainlineVersionCalculator(
        IIncrementStrategyFinder incrementStrategyFinder, MainlineBranchPointResolver branchPointResolver,
        MainlineConfigurationResolver configurationResolver, MainlineHistory history)
    {
        IncrementStrategyFinder = incrementStrategyFinder.NotNull();
        BranchPointResolver = branchPointResolver.NotNull();
        ConfigurationResolver = configurationResolver.NotNull();
        this.history = history.NotNull();
    }

    internal IIncrementStrategyFinder IncrementStrategyFinder { get; }

    internal MainlineBranchPointResolver BranchPointResolver { get; }

    internal MainlineConfigurationResolver ConfigurationResolver { get; }

    internal MainlineHistory History => this.history;

    internal MainlineContext CreateContext(string? targetLabel = null)
        => new(this) { TargetLabel = targetLabel };

    private static readonly IReadOnlyCollection<IContextPreEnricher> TrunkContextPreEnricherCollection =
    [
        new EnrichSemanticVersion(),
        new EnrichIncrement(),
        new EnrichMergeState()
    ];
    private static readonly IReadOnlyCollection<IContextPostEnricher> TrunkContextPostEnricherCollection =
    [
        new RemoveSemanticVersion(),
        new RemoveIncrement()
    ];
    private static readonly IReadOnlyCollection<IIncrementer> TrunkIncrementerCollection =
        MergedMainlineIncrementer.Decorate(
    [
        // Trunk
        new CommitOnTrunk(),

        new CommitOnTrunkWithPreReleaseTag(),
        new LastCommitOnTrunkWithPreReleaseTag(),

        new CommitOnTrunkWithStableTag(),
        new LastCommitOnTrunkWithStableTag(),

        new MergeCommitOnTrunk(),
        new LastMergeCommitOnTrunk(),

        new CommitOnTrunkBranchedToTrunk(),
        new CommitOnTrunkBranchedToNonTrunk(),

        // NonTrunk
        new FirstCommitOnRelease(),

        new CommitOnNonTrunk(),
        new CommitOnNonTrunkWithPreReleaseTag(),
        new LastCommitOnNonTrunkWithPreReleaseTag(),

        new CommitOnNonTrunkWithStableTag(),
        new LastCommitOnNonTrunkWithStableTag(),

        new MergeCommitOnNonTrunk(),
        new LastMergeCommitOnNonTrunk(),

        new CommitOnNonTrunkBranchedToTrunk(),
        new CommitOnNonTrunkBranchedToNonTrunk()
    ]);

    internal BaseVersion DetermineBaseVersion(MainlineIteration iteration, string? targetLabel)
        => CalculateBaseVersion(iteration, CreateContext(targetLabel));

    internal BaseVersion CalculateBaseVersion(MainlineIteration iteration, MainlineContext context,
        bool includeAncestry = false)
    {
        iteration.NotNull();
        var incrementSteps = GetIncrements(iteration, context, includeAncestry).ToArray();
        return ApplyIncrements(incrementSteps);
    }

    private static BaseVersion ApplyIncrements(IEnumerable<IBaseVersionIncrement> incrementSteps)
    {
        BaseVersion? result = null;
        foreach (var baseVersionIncrement in incrementSteps)
        {
            result = MainlineIncrementState.Apply(result, baseVersionIncrement);
        }
        return result ?? throw new InvalidOperationException();
    }

    private IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineContext context, bool includeAncestry)
    {
        var (commits, _) = GetCalculationCommits(iteration, context, includeAncestry);
        for (var index = 0; index < commits.Length; index++)
        {
            var commit = commits[index];
            context.Predecessor = index > 0 ? commits[index - 1] : null;
            context.HasSuccessor = index < commits.Length - 1;
            foreach (var item in TrunkContextPreEnricherCollection)
            {
                item.Enrich(iteration, commit, context);
            }

            foreach (var incrementer in TrunkIncrementerCollection
                .Where(element => element.MatchPrecondition(iteration, commit, context)))
            {
                foreach (var item in incrementer.GetIncrements(iteration, commit, context))
                {
                    context.Increments.Add(item);
                    yield return item;
                }
            }

            foreach (var item in TrunkContextPostEnricherCollection)
            {
                item.Enrich(commit, context);
            }
        }
    }

    private (MainlineCommit[] Commits, bool HasBoundary) GetCalculationCommits(MainlineIteration iteration,
        MainlineContext context, bool includeAncestry)
    {
        var selection = this.history.Select(iteration, context.TargetLabel, includeAncestry,
            includeSharedHistory: context.IncludeSharedHistory);
        if (selection.MissingCommit is { } missing)
        {
            throw new InvalidOperationException(
                $"The recorded mainline history does not contain branch point '{missing.Sha}'.");
        }
        return (selection.Commits, selection.HasBoundary);
    }
}
