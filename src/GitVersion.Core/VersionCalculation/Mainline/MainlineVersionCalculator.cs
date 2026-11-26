using GitVersion.Extensions;
using GitVersion.VersionCalculation.Mainline.NonTrunk;
using GitVersion.VersionCalculation.Mainline.Trunk;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class MainlineVersionCalculator
{
    private readonly MainlineHistory history;
    private readonly IReadOnlyCollection<IContextPreEnricher> preEnrichers;
    private readonly IReadOnlyCollection<IIncrementer> incrementers;

    private static readonly IReadOnlyCollection<IContextPostEnricher> PostEnrichers =
    [
        new RemoveSemanticVersion(),
        new RemoveIncrement()
    ];

    internal MainlineVersionCalculator(
        IIncrementStrategyFinder incrementStrategyFinder,
        MainlineConfigurationResolver configurationResolver, MainlineHistory history)
    {
        incrementStrategyFinder.NotNull();
        configurationResolver.NotNull();
        this.history = history.NotNull();
        var environment = history.Environment;
        var sourceVersions = new MergedMainlineVersionPolicy(configurationResolver, environment, this);

        this.preEnrichers =
        [
            new ConfigurationPreEnricher(configurationResolver),
            new EnrichSemanticVersion(environment),
            new EnrichIncrement(incrementStrategyFinder, history.Configuration, configurationResolver, environment),
            new EnrichMergeState()
        ];
        this.incrementers =
        [
            // Trunk
            new CommitOnTrunk(history, environment),
            new MergedMainlineIncrementer(history, environment, sourceVersions),

            new CommitOnTrunkWithPreReleaseTag(),
            new LastCommitOnTrunkWithPreReleaseTag(environment),

            new CommitOnTrunkWithStableTag(environment),
            new LastCommitOnTrunkWithStableTag(environment),

            new MergeCommitOnTrunk(history, this),
            new LastMergeCommitOnTrunk(history, this),

            new CommitOnTrunkBranchedToTrunk(environment),
            new MergedMainlineBranchedToTrunk(environment, sourceVersions),
            new CommitOnTrunkBranchedToNonTrunk(environment),
            new MergedMainlineBranchedToNonTrunk(environment, sourceVersions),

            // NonTrunk
            new FirstCommitOnRelease(history),

            new CommitOnNonTrunk(history, environment),
            new CommitOnNonTrunkWithPreReleaseTag(environment),
            new LastCommitOnNonTrunkWithPreReleaseTag(environment),

            new CommitOnNonTrunkWithStableTag(environment),
            new LastCommitOnNonTrunkWithStableTag(environment),

            new MergeCommitOnNonTrunk(history, this),
            new LastMergeCommitOnNonTrunk(history, this),

            new CommitOnNonTrunkBranchedToTrunk(environment),
            new CommitOnNonTrunkBranchedToNonTrunk(environment)
        ];
    }

    internal BaseVersion DetermineBaseVersion(MainlineIteration iteration, string? targetLabel)
        => CalculateBaseVersion(iteration, targetLabel, [], includeSharedHistory: false);

    internal BaseVersion CalculateBaseVersion(MainlineIteration iteration, string? targetLabel,
        Dictionary<MainlineIteration, BaseVersion> mainlineVersions, bool includeSharedHistory, bool includeAncestry = false)
    {
        iteration.NotNull();
        var context = new MainlineContext(this.history.CurrentCommit, mainlineVersions)
        {
            TargetLabel = targetLabel,
            IncludeSharedHistory = includeSharedHistory
        };
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
            foreach (var item in this.preEnrichers)
            {
                item.Enrich(iteration, commit, context);
            }

            foreach (var incrementer in this.incrementers
                .Where(element => element.MatchPrecondition(iteration, commit, context)))
            {
                foreach (var item in incrementer.GetIncrements(iteration, commit, context))
                {
                    context.Increments.Add(item);
                    yield return item;
                }
            }

            foreach (var item in PostEnrichers)
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
