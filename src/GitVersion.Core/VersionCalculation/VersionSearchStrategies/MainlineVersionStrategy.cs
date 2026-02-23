using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.VersionCalculation.Mainline;
using GitVersion.VersionCalculation.Mainline.NonTrunk;
using GitVersion.VersionCalculation.Mainline.Trunk;

namespace GitVersion.VersionCalculation;

internal sealed class MainlineVersionStrategy(
    Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore,
    ITaggedSemanticVersionService taggedSemanticVersionService,
    IIncrementStrategyFinder incrementStrategyFinder,
    IEnvironment environment)
    : IVersionStrategy
{
    private volatile int iterationCounter;
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly ITaggedSemanticVersionService taggedSemanticVersionService = taggedSemanticVersionService.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();
    private readonly IEnvironment environment = environment.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    private static readonly IReadOnlyCollection<IContextPreEnricher> TrunkContextPreEnricherCollection =
    [
        new EnrichSemanticVersion(),
        new EnrichIncrement()
    ];
    private static readonly IReadOnlyCollection<IContextPostEnricher> TrunkContextPostEnricherCollection =
    [
        new RemoveSemanticVersion(),
        new RemoveIncrement()
    ];
    private static readonly IReadOnlyCollection<IIncrementer> TrunkIncrementerCollection =
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
    ];

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.Mainline))
            yield break;

        var branchConfiguration = Context.Configuration.GetBranchConfiguration(Context.CurrentBranch);

        var iteration = CreateIteration(
            branchName: Context.CurrentBranch.Name,
            configuration: branchConfiguration
        );

        var commitsInReverseOrder = Context.Configuration.Ignore.Filter(Context.CurrentBranchCommits.ToArray());

        var taggedSemanticVersion = TaggedSemanticVersions.OfBranch;
        if (branchConfiguration.TrackMergeTarget == true) taggedSemanticVersion |= TaggedSemanticVersions.OfMergeTargets;
        if (branchConfiguration.TracksReleaseBranches == true)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfReleaseBranches;
        }
        if (!(branchConfiguration.IsMainBranch == true || branchConfiguration.IsReleaseBranch == true))
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMainBranches;
        }
        var taggedSemanticVersions = taggedSemanticVersionService.GetTaggedSemanticVersions(
            branch: Context.CurrentBranch,
            configuration: Context.Configuration,
            label: null,
            notOlderThan: Context.CurrentCommit.When,
            taggedSemanticVersion: taggedSemanticVersion
        );
        var targetLabel = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null, this.environment);
        IterateOverCommitsRecursive(
            commitsInReverseOrder: commitsInReverseOrder,
            iteration: iteration,
            targetBranch: configuration.Branch,
            targetLabel: targetLabel,
            taggedSemanticVersions: taggedSemanticVersions
        );

        yield return DetermineBaseVersion(iteration, targetLabel, incrementStrategyFinder, Context.Configuration, this.environment);
    }

    private MainlineIteration CreateIteration(
        ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration = null, MainlineCommit? parentCommit = null)
    {
        var iterationCount = Interlocked.Increment(ref iterationCounter);
        return new MainlineIteration(
            id: $"#{iterationCount}",
            branchName: branchName,
            configuration: configuration,
            parentIteration: parentIteration,
            parentCommit: parentCommit
        );
    }

    private bool IterateOverCommitsRecursive(
        IEnumerable<ICommit> commitsInReverseOrder, MainlineIteration iteration, IBranch targetBranch, string? targetLabel,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions, HashSet<ICommit>? traversedCommits = null)
    {
        traversedCommits ??= [];

        var returnTrueWhenTheIncrementIsKnown = false;

        var configuration = iteration.Configuration;
        var branchName = iteration.BranchName;
        var branch = repositoryStore.FindBranch(branchName);

        var currentBranch = branch;
        Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> commitsWasBranchedFromLazy = new(
            () => currentBranch is null ? [] : GetCommitsWasBranchedFrom(currentBranch)
        );

        foreach (var item in commitsInReverseOrder)
        {
            if (!traversedCommits.Add(item)) continue;

            if (commitsWasBranchedFromLazy.Value.TryGetValue(item, out var effectiveConfigurationsWasBranchedFrom))
            {
                var effectiveConfigurationWasBranchedFrom = effectiveConfigurationsWasBranchedFrom[0];

                if (configuration.IsMainBranch != true || effectiveConfigurationWasBranchedFrom.Value.IsMainBranch == true)
                {
                    var excludeBranch = branch;
                    if (effectiveConfigurationsWasBranchedFrom.Any(element =>
                        !element.Branch.Equals(effectiveConfigurationWasBranchedFrom.Branch)
                            && element.Branch.Equals(targetBranch)))
                    {
                        iteration.CreateCommit(null, targetBranch.Name, Context.Configuration.GetBranchConfiguration(targetBranch));
                    }
                    configuration = effectiveConfigurationWasBranchedFrom.Value;
                    branchName = effectiveConfigurationWasBranchedFrom.Branch.Name;

                    branch = repositoryStore.FindBranch(branchName);

                    var branchPointer = branch;
                    commitsWasBranchedFromLazy = new Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Configuration)>>>
                        (() => branchPointer is null ? []
                            : GetCommitsWasBranchedFrom(branchPointer, excludeBranch is null ? [] : [excludeBranch])
                    );

                    var taggedSemanticVersion = TaggedSemanticVersions.OfBranch;
                    if ((configuration.TrackMergeTarget ?? Context.Configuration.TrackMergeTarget) == true)
                    {
                        taggedSemanticVersion |= TaggedSemanticVersions.OfMergeTargets;
                    }
                    if ((configuration.TracksReleaseBranches ?? Context.Configuration.TracksReleaseBranches) == true)
                    {
                        taggedSemanticVersion |= TaggedSemanticVersions.OfReleaseBranches;
                    }
                    if (!(configuration.IsMainBranch == true || configuration.IsReleaseBranch == true))
                    {
                        taggedSemanticVersion |= TaggedSemanticVersions.OfMainBranches;
                    }
                    taggedSemanticVersions = taggedSemanticVersionService.GetTaggedSemanticVersions(
                        branch: effectiveConfigurationWasBranchedFrom.Branch,
                        configuration: Context.Configuration,
                        label: null,
                        notOlderThan: Context.CurrentCommit.When,
                        taggedSemanticVersion: taggedSemanticVersion
                    );
                }
            }

            var commit = iteration.CreateCommit(item, branchName, configuration);

            var semanticVersions = taggedSemanticVersions[item].ToArray();
            commit.AddSemanticVersions(semanticVersions.Select(element => element.Value));

            var label = targetLabel ?? new EffectiveConfiguration(
                configuration: Context.Configuration,
                branchConfiguration: configuration
            ).GetBranchSpecificLabel(branchName, null, this.environment);

            foreach (var semanticVersion in semanticVersions)
            {
                if (!semanticVersion.Value.IsMatchForBranchSpecificLabel(label)) continue;
                if (configuration.Increment != IncrementStrategy.Inherit)
                {
                    return true;
                }

                returnTrueWhenTheIncrementIsKnown = true;
            }

            if (returnTrueWhenTheIncrementIsKnown && configuration.Increment != IncrementStrategy.Inherit)
            {
                return true;
            }

            if (!item.IsMergeCommit) continue;
            Lazy<IReadOnlyCollection<ICommit>> mergedCommitsInReverseOrderLazy = new(
                () => [.. this.incrementStrategyFinder.GetMergedCommits(item, 1, Context.Configuration.Ignore).Reverse()]
            );

            if ((configuration.TrackMergeMessage ?? Context.Configuration.TrackMergeMessage) != true
                || !MergeMessage.TryParse(item, Context.Configuration, out var mergeMessage))
            {
                continue;
            }

            if (mergeMessage.MergedBranch is not null)
            {
                var childConfiguration = Context.Configuration.GetBranchConfiguration(mergeMessage.MergedBranch);
                var childBranchName = mergeMessage.MergedBranch;

                if (childConfiguration.IsMainBranch == true)
                {
                    if (configuration.IsMainBranch == true) throw new NotImplementedException();

                    mergedCommitsInReverseOrderLazy = new(
                        () => [.. this.incrementStrategyFinder.GetMergedCommits(item, 0, Context.Configuration.Ignore).Reverse()]
                    );
                    childConfiguration = configuration;
                    childBranchName = iteration.BranchName;
                }

                var childIteration = CreateIteration(
                    branchName: childBranchName,
                    configuration: childConfiguration,
                    parentIteration: iteration,
                    parentCommit: commit
                );

                var done = IterateOverCommitsRecursive(
                    commitsInReverseOrder: mergedCommitsInReverseOrderLazy.Value,
                    iteration: childIteration,
                    targetBranch: targetBranch,
                    targetLabel: targetLabel,
                    taggedSemanticVersions: taggedSemanticVersions,
                    traversedCommits: traversedCommits);

                commit.AddChildIteration(childIteration);
                if (done) return true;
            }

            traversedCommits.AddRange(mergedCommitsInReverseOrderLazy.Value);
        }
        return false;
    }

    private Dictionary<ICommit, List<(IBranch, IBranchConfiguration)>> GetCommitsWasBranchedFrom(
        IBranch branch, params IBranch[] excludedBranches)
    {
        Dictionary<ICommit, List<(IBranch, IBranchConfiguration Configuration)>> result = [];

        var branchCommits = repositoryStore.FindCommitBranchesBranchedFrom(
            branch, Context.Configuration, excludedBranches: excludedBranches
        ).ToList();

        var branchCommitDictionary = branchCommits.ToDictionary(
            element => element.Branch, element => element.Commit
        );
        foreach (var item in branchCommitDictionary.Keys)
        {
            var branchConfiguration = Context.Configuration.GetBranchConfiguration(item);

            var key = branchCommitDictionary[item];
            if (result.TryGetValue(key, out var value))
            {
                if (branchConfiguration is { Increment: IncrementStrategy.Inherit, IsMainBranch: null })
                {
                    throw new InvalidOperationException();
                }

                if ((branchConfiguration.IsMainBranch ?? Context.Configuration.IsMainBranch) != true) continue;
                foreach (var _ in value.ToArray())
                {
                    value.Add(new(item, branchConfiguration));
                }
            }
            else
            {
                result.Add(key: key, value: [new ValueTuple<IBranch, IBranchConfiguration>(item, branchConfiguration)]);
            }
        }

        // If a main branch existing we need to ensure that it will be present at the first position in the list.
        foreach (var item in result)
        {
            result[item.Key] = [.. item.Value.OrderByDescending(element => (element.Configuration.IsMainBranch ?? Context.Configuration.IsMainBranch) == true)];
        }
        return result;
    }

    private static BaseVersion DetermineBaseVersion(MainlineIteration iteration, string? targetLabel,
            IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment)
        => DetermineBaseVersionRecursive(iteration, targetLabel, incrementStrategyFinder, configuration, environment);

    internal static BaseVersion DetermineBaseVersionRecursive(MainlineIteration iteration, string? targetLabel,
        IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment)
    {
        iteration.NotNull();

        var incrementSteps = GetIncrements(iteration, targetLabel, incrementStrategyFinder, configuration, environment).ToArray();

        BaseVersion? result = null;
        foreach (var baseVersionIncrement in incrementSteps)
        {
            switch (baseVersionIncrement)
            {
                case BaseVersionOperand baseVersionOperand:
                    result = new BaseVersion(baseVersionOperand);
                    break;
                case BaseVersionOperator baseVersionOperator:
                    result ??= new BaseVersion();
                    result = result.Apply(baseVersionOperator);
                    break;
                case BaseVersion baseVersion:
                    result = baseVersion;
                    break;
            }
        }
        return result ?? throw new InvalidOperationException();
    }

    private static IEnumerable<IBaseVersionIncrement> GetIncrements(MainlineIteration iteration, string? targetLabel,
        IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment)
    {
        MainlineContext context = new(incrementStrategyFinder, configuration, environment)
        {
            TargetLabel = targetLabel
        };

        foreach (var commit in iteration.Commits)
        {
            foreach (var item in TrunkContextPreEnricherCollection)
            {
                item.Enrich(iteration, commit, context);
            }

            foreach (var incrementer in TrunkIncrementerCollection
                .Where(element => element.MatchPrecondition(iteration, commit, context)))
            {
                foreach (var item in incrementer.GetIncrements(iteration, commit, context))
                {
                    yield return item;
                }
            }

            foreach (var item in TrunkContextPostEnricherCollection)
            {
                item.Enrich(commit, context);
            }
        }
    }
}
