using GitVersion.Configuration;
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
    private readonly Dictionary<string, Dictionary<ICommit, List<(IBranch, IBranchConfiguration)>>> commitsWasBranchedFromCache = new();

    private GitVersionContext Context => this.contextLazy.Value;

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
        {
            yield break;
        }

        var branchConfiguration = Context.Configuration.GetBranchConfiguration(Context.CurrentBranch);

        var iteration = CreateIteration(
            branchName: Context.CurrentBranch.Name,
            configuration: branchConfiguration
        );

        var commitsInReverseOrder = Context.Configuration.Ignore.Filter(Context.CurrentBranchCommits.ToArray());

        var taggedSemanticVersion = TaggedSemanticVersions.OfBranch;
        if (branchConfiguration.TrackMergeTarget == true)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMergeTargets;
        }

        if (branchConfiguration.TracksReleaseBranches == true)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfReleaseBranches;
        }
        if (!(branchConfiguration.IsMainBranch == true || branchConfiguration.IsReleaseBranch == true))
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMainBranches;
        }
        var taggedSemanticVersions = this.taggedSemanticVersionService.GetTaggedSemanticVersions(
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

        yield return DetermineBaseVersion(iteration, targetLabel, this.incrementStrategyFinder, Context.Configuration, this.environment);
    }

    private MainlineIteration CreateIteration(
        ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration = null, MainlineCommit? parentCommit = null)
    {
        var iterationCount = Interlocked.Increment(ref this.iterationCounter);
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

        var branch = this.repositoryStore.FindBranch(iteration.BranchName);
        var currentBranch = branch;
        TraversalState state = new(
            configuration: iteration.Configuration,
            branchName: iteration.BranchName,
            branch: branch,
            taggedSemanticVersions: taggedSemanticVersions,
            commitsWasBranchedFromLazy: new(() => currentBranch is null ? [] : GetCommitsWasBranchedFrom(currentBranch))
        );

        foreach (var item in commitsInReverseOrder)
        {
            if (!traversedCommits.Add(item))
            {
                continue;
            }

            ApplyBranchedFromTransition(item, iteration, targetBranch, state);

            var (commit, stop) = ProcessCommit(item, iteration, targetLabel, state);
            if (stop)
            {
                return true;
            }

            if (item.IsMergeCommit
                && HandleMergeCommit(item, commit, iteration, targetBranch, targetLabel, state, traversedCommits))
            {
                return true;
            }
        }
        return false;
    }

    private void ApplyBranchedFromTransition(
        ICommit item, MainlineIteration iteration, IBranch targetBranch, TraversalState state)
    {
        if (!state.CommitsWasBranchedFromLazy.Value.TryGetValue(item, out var effectiveConfigurationsWasBranchedFrom))
        {
            return;
        }

        var effectiveConfigurationWasBranchedFrom = effectiveConfigurationsWasBranchedFrom[0];

        if (state.Configuration.IsMainBranch == true && effectiveConfigurationWasBranchedFrom.Value.IsMainBranch != true)
        {
            return;
        }

        var excludeBranch = state.Branch;
        if (effectiveConfigurationsWasBranchedFrom.Any(element =>
            !element.Branch.Equals(effectiveConfigurationWasBranchedFrom.Branch)
                && element.Branch.Equals(targetBranch)))
        {
            iteration.CreateCommit(null, targetBranch.Name, Context.Configuration.GetBranchConfiguration(targetBranch));
        }

        state.Configuration = effectiveConfigurationWasBranchedFrom.Value;
        state.BranchName = effectiveConfigurationWasBranchedFrom.Branch.Name;
        state.Branch = this.repositoryStore.FindBranch(state.BranchName);

        var branchPointer = state.Branch;
        IBranch[] excludeBranches = excludeBranch is null ? [] : [excludeBranch];
        state.CommitsWasBranchedFromLazy = new(
            () => branchPointer is null ? [] : GetCommitsWasBranchedFrom(branchPointer, excludeBranches)
        );

        var taggedSemanticVersion = TaggedSemanticVersions.OfBranch;
        if ((state.Configuration.TrackMergeTarget ?? Context.Configuration.TrackMergeTarget) == true)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMergeTargets;
        }
        if ((state.Configuration.TracksReleaseBranches ?? Context.Configuration.TracksReleaseBranches) == true)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfReleaseBranches;
        }
        if (!(state.Configuration.IsMainBranch == true || state.Configuration.IsReleaseBranch == true))
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMainBranches;
        }
        state.TaggedSemanticVersions = this.taggedSemanticVersionService.GetTaggedSemanticVersions(
            branch: effectiveConfigurationWasBranchedFrom.Branch,
            configuration: Context.Configuration,
            label: null,
            notOlderThan: Context.CurrentCommit.When,
            taggedSemanticVersion: taggedSemanticVersion
        );
    }

    private (MainlineCommit Commit, bool Stop) ProcessCommit(
        ICommit item, MainlineIteration iteration, string? targetLabel, TraversalState state)
    {
        var commit = iteration.CreateCommit(item, state.BranchName, state.Configuration);

        var semanticVersions = state.TaggedSemanticVersions[item].ToArray();
        commit.AddSemanticVersions(semanticVersions.Select(element => element.Value));

        var label = targetLabel ?? new EffectiveConfiguration(
            configuration: Context.Configuration,
            branchConfiguration: state.Configuration
        ).GetBranchSpecificLabel(state.BranchName, null, this.environment);

        foreach (var semanticVersion in semanticVersions)
        {
            if (!semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
            {
                continue;
            }

            if (state.Configuration.Increment != IncrementStrategy.Inherit)
            {
                return (commit, true);
            }

            state.ReturnTrueWhenTheIncrementIsKnown = true;
        }

        if (state.ReturnTrueWhenTheIncrementIsKnown && state.Configuration.Increment != IncrementStrategy.Inherit)
        {
            return (commit, true);
        }

        return (commit, false);
    }

    private bool HandleMergeCommit(
        ICommit item, MainlineCommit commit, MainlineIteration iteration, IBranch targetBranch,
        string? targetLabel, TraversalState state, HashSet<ICommit> traversedCommits)
    {
        Lazy<IReadOnlyCollection<ICommit>> mergedCommitsInReverseOrderLazy = new(
            () => [.. this.incrementStrategyFinder.GetMergedCommits(item, 1, Context.Configuration.Ignore).Reverse()]
        );

        if ((state.Configuration.TrackMergeMessage ?? Context.Configuration.TrackMergeMessage) != true
            || !MergeMessage.TryParse(item, Context.Configuration, out var mergeMessage))
        {
            return false;
        }

        if (mergeMessage.MergedBranch is null || mergeMessage.MergedBranch.EquivalentTo(state.BranchName.WithoutOrigin))
        {
            return false;
        }

        var childConfiguration = Context.Configuration.GetBranchConfiguration(mergeMessage.MergedBranch);
        var childBranchName = mergeMessage.MergedBranch;

        if (childConfiguration.IsMainBranch == true)
        {
            if (state.Configuration.IsMainBranch == true)
            {
                throw new NotImplementedException();
            }

            mergedCommitsInReverseOrderLazy = new(
                () => [.. this.incrementStrategyFinder.GetMergedCommits(item, 0, Context.Configuration.Ignore).Reverse()]
            );
            childConfiguration = state.Configuration;
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
            taggedSemanticVersions: state.TaggedSemanticVersions,
            traversedCommits: traversedCommits);

        commit.AddChildIteration(childIteration);
        if (done)
        {
            return true;
        }

        traversedCommits.AddRange(mergedCommitsInReverseOrderLazy.Value);
        return false;
    }

    private sealed class TraversalState(
        IBranchConfiguration configuration,
        ReferenceName branchName,
        IBranch? branch,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions,
        Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> commitsWasBranchedFromLazy)
    {
        public IBranchConfiguration Configuration { get; set; } = configuration;
        public ReferenceName BranchName { get; set; } = branchName;
        public IBranch? Branch { get; set; } = branch;
        public ILookup<ICommit, SemanticVersionWithTag> TaggedSemanticVersions { get; set; } = taggedSemanticVersions;
        public Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> CommitsWasBranchedFromLazy { get; set; } = commitsWasBranchedFromLazy;
        public bool ReturnTrueWhenTheIncrementIsKnown { get; set; }
    }

    private Dictionary<ICommit, List<(IBranch, IBranchConfiguration)>> GetCommitsWasBranchedFrom(
        IBranch branch, params IBranch[] excludedBranches)
    {
        // Create cache key from canonical branch name and canonical excluded branch names
        var cacheKey = $"{branch.Name.Canonical}|{string.Join(",", excludedBranches.Select(b => b.Name.Canonical).OrderBy(n => n))}";

        // Return cached result if available
        if (this.commitsWasBranchedFromCache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        Dictionary<ICommit, List<(IBranch, IBranchConfiguration Configuration)>> result = [];

        var branchCommits = this.repositoryStore.FindCommitBranchesBranchedFrom(
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

                // Fix: Just add the item once instead of duplicating for each existing item
                // The original logic caused exponential growth: 1→2→4→8→16 with multiple branches
                if ((branchConfiguration.IsMainBranch ?? Context.Configuration.IsMainBranch).GetValueOrDefault())
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

        // Cache the result for future calls
        this.commitsWasBranchedFromCache[cacheKey] = result;

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
