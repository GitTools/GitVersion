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
    private readonly Dictionary<ICommit, MainlineCommit> recordedCommits = new();
    private readonly List<(MainlineIteration Iteration, ICommit Tip)> mergedIterations = [];

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

        var iteration = BuildIteration(configuration, out var targetLabel);

        yield return DetermineBaseVersion(iteration, targetLabel, this.incrementStrategyFinder, Context.Configuration, this.environment, Context.CurrentCommit);
    }

    internal MainlineIteration BuildIteration(EffectiveBranchConfiguration configuration, out string? targetLabel)
    {
        this.recordedCommits.Clear();
        this.mergedIterations.Clear();
        var branchConfiguration = Context.Configuration.GetBranchConfiguration(Context.CurrentBranch);

        var iteration = CreateIteration(
            branchName: Context.CurrentBranch.Name,
            configuration: branchConfiguration
        );

        var commitsInReverseOrder = Context.Configuration.Ignore.Filter(Context.CurrentBranchCommits);

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
        targetLabel = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null, this.environment, Context.CurrentCommit);
        IterateOverCommitsRecursive(
            commitsInReverseOrder: commitsInReverseOrder,
            iteration: iteration,
            targetBranch: configuration.Branch,
            taggedSemanticVersions: taggedSemanticVersions
        );

        ResolveBranchPoints();
        return iteration;
    }

    private void ResolveBranchPoints()
    {
        foreach (var (child, tip) in this.mergedIterations)
        {
            var ancestor = tip;
            while (ancestor is not null)
            {
                if (this.recordedCommits.TryGetValue(ancestor, out var recorded) && !ReferenceEquals(recorded.Iteration, child))
                {
                    child.BranchedFrom = recorded;
                    break;
                }
                ancestor = ancestor.Parents.FirstOrDefault();
            }
        }
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

    private void IterateOverCommitsRecursive(
        IEnumerable<ICommit> commitsInReverseOrder, MainlineIteration iteration, IBranch targetBranch,
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
            commitsWasBranchedFromLazy: new(() => currentBranch is null ? [] : GetCommitsWasBranchedFrom(currentBranch)),
            traversedCommits: traversedCommits
        );

        foreach (var item in commitsInReverseOrder)
        {
            if (state.ChildCommits.Contains(item) || !traversedCommits.Add(item))
            {
                continue;
            }

            ApplyBranchedFromTransition(item, iteration, targetBranch, state);

            var commit = ProcessCommit(item, iteration, state);

            if (item.IsMergeCommit)
            {
                HandleMergeCommit(item, commit, iteration, targetBranch, state);
            }
        }
        // Record the receiver first so nested merges reference its history
        // instead of taking ownership of commits on its ancestry.
        foreach (var buildChild in state.PendingChildren)
        {
            buildChild();
        }
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

    private MainlineCommit ProcessCommit(ICommit item, MainlineIteration iteration, TraversalState state)
    {
        var commit = iteration.CreateCommit(item, state.BranchName, state.Configuration);
        this.recordedCommits.Add(item, commit);

        var semanticVersions = state.TaggedSemanticVersions[item].ToArray();
        commit.AddSemanticVersions(semanticVersions.Select(element => element.Value));

        return commit;
    }

    private void HandleMergeCommit(
        ICommit item, MainlineCommit commit, MainlineIteration iteration, IBranch targetBranch,
        TraversalState state)
    {
        Lazy<IReadOnlyCollection<ICommit>> mergedCommitsInReverseOrderLazy = new(
            () => this.repositoryStore.GetCommitLog(item.Parents[0], item.Parents[1], Context.Configuration.Ignore)
        );

        if ((state.Configuration.TrackMergeMessage ?? Context.Configuration.TrackMergeMessage) != true
            || !MergeMessage.TryParse(item, Context.Configuration, out var mergeMessage))
        {
            return;
        }

        if (mergeMessage.MergedBranch is null || mergeMessage.MergedBranch.EquivalentTo(state.BranchName.WithoutOrigin))
        {
            return;
        }

        var childConfiguration = Context.Configuration.GetBranchConfiguration(mergeMessage.MergedBranch);
        var childBranchName = mergeMessage.MergedBranch;

        if (item.Parents.Count > 2)
        {
            // Report unsupported history only if calculation reaches this merge;
            // an eligible release tag can make older merges irrelevant.
            commit.HasUnsupportedMerge = true;
            return;
        }
        var sourceTip = item.Parents[1];
        if (childConfiguration.IsMainBranch == true && state.Configuration.IsMainBranch != true)
        {
            mergedCommitsInReverseOrderLazy = new(
                () => this.repositoryStore.GetCommitLog(item.Parents[1], item.Parents[0], Context.Configuration.Ignore)
            );
            childConfiguration = state.Configuration;
            childBranchName = iteration.BranchName;
            sourceTip = item.Parents[0];
        }

        var childIteration = CreateIteration(
            branchName: childBranchName,
            configuration: childConfiguration,
            parentIteration: iteration,
            parentCommit: commit
        );

        this.mergedIterations.Add((childIteration, sourceTip));
        var tags = state.TaggedSemanticVersions;
        state.PendingChildren.Add(() => IterateOverCommitsRecursive(
                commitsInReverseOrder: mergedCommitsInReverseOrderLazy.Value,
                iteration: childIteration,
                targetBranch: targetBranch,
                taggedSemanticVersions: tags,
                traversedCommits: state.TraversedCommits));

        commit.AddChildIteration(childIteration);
        state.ChildCommits.AddRange(mergedCommitsInReverseOrderLazy.Value);
    }

    internal static BaseVersion GetMergedMainlineVersion(MainlineIteration iteration, MainlineContext context)
    {
        // Inherited configurations depend on their enclosing iteration.
        var canCache = iteration.Configuration.Increment != IncrementStrategy.Inherit;
        if (canCache && context.MainlineVersions.TryGetValue(iteration, out var cachedVersion))
        {
            return cachedVersion;
        }
        var version = CalculateMergedMainlineVersion(iteration, context);
        if (canCache)
        {
            context.MainlineVersions.Add(iteration, version);
        }
        return version;
    }

    private static BaseVersion CalculateMergedMainlineVersion(MainlineIteration iteration, MainlineContext context)
    {
        // The source tip can be ignored; the last recorded commit may be an
        // earlier tag and must not be mistaken for a released source tip.
        var tip = iteration.ParentCommit.NotNull().Value.Parents[1];
        var configuration = iteration.GetEffectiveConfiguration(context.Configuration);
        var label = configuration.GetBranchSpecificLabel(iteration.BranchName, null, context.Environment, tip);
        MainlineContext sourceContext = new(context.IncrementStrategyFinder, context.Configuration, context.Environment, context.CurrentCommit)
        {
            TargetLabel = label,
            MainlineVersions = context.MainlineVersions
        };
        var version = ApplyIncrements([.. GetIncrements(iteration, sourceContext, includeAncestry: true)]);
        // A released source tip contributes its tag, not its next pending increment.
        if (version.Operand.BaseVersionSource?.Equals(tip) == true
            && configuration.PreventIncrementWhenCurrentCommitTagged
            && version.Operator is { } operation)
        {
            version = version with { Operator = operation with { Increment = VersionField.None } };
        }
        return version;
    }

    private sealed class TraversalState(
        IBranchConfiguration configuration,
        ReferenceName branchName,
        IBranch? branch,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions,
        Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> commitsWasBranchedFromLazy,
        HashSet<ICommit> traversedCommits)
    {
        public IBranchConfiguration Configuration { get; set; } = configuration;
        public ReferenceName BranchName { get; set; } = branchName;
        public IBranch? Branch { get; set; } = branch;
        public ILookup<ICommit, SemanticVersionWithTag> TaggedSemanticVersions { get; set; } = taggedSemanticVersions;
        public HashSet<ICommit> TraversedCommits { get; } = traversedCommits;
        public HashSet<ICommit> ChildCommits { get; } = [];
        public List<Action> PendingChildren { get; } = [];
        public Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> CommitsWasBranchedFromLazy { get; set; } = commitsWasBranchedFromLazy;
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
            IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
        => DetermineBaseVersionRecursive(iteration, targetLabel, incrementStrategyFinder, configuration, environment, currentCommit);

    internal static BaseVersion DetermineBaseVersionRecursive(MainlineIteration iteration, string? targetLabel,
        IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit,
        Dictionary<MainlineIteration, BaseVersion>? mainlineVersions = null)
    {
        iteration.NotNull();

        MainlineContext context = new(incrementStrategyFinder, configuration, environment, currentCommit)
        {
            TargetLabel = targetLabel,
            MainlineVersions = mainlineVersions ?? []
        };
        var incrementSteps = GetIncrements(iteration, context, includeAncestry: false).ToArray();
        return ApplyIncrements(incrementSteps);
    }

    private static BaseVersion ApplyIncrements(IEnumerable<IBaseVersionIncrement> incrementSteps)
    {
        BaseVersion? result = null;
        SemanticVersion? iterationBaseline = null;
        foreach (var baseVersionIncrement in incrementSteps)
        {
            switch (baseVersionIncrement)
            {
                case BaseVersionOperand baseVersionOperand:
                    result = new BaseVersion(baseVersionOperand);
                    iterationBaseline = baseVersionOperand.SemanticVersion;
                    break;
                case BaseVersionOperator baseVersionOperator:
                    result ??= new BaseVersion();
                    result = result.Apply(baseVersionOperator);
                    break;
                case BaseVersion baseVersion:
                    result = baseVersion;
                    iterationBaseline = baseVersion.SemanticVersion;
                    break;
                case MergedMainlineIncrement merged:
                    result = ApplyMergedMainlineIncrement(result ?? new BaseVersion(), merged, iterationBaseline);
                    break;
            }
        }
        return result ?? throw new InvalidOperationException();
    }

    private static IEnumerable<IBaseVersionIncrement> GetIncrements(MainlineIteration iteration, MainlineContext context, bool includeAncestry)
    {
        var (commits, _) = GetCalculationCommits(iteration, context, includeAncestry);
        for (var index = 0; index < commits.Length; index++)
        {
            var commit = commits[index];
            context.Predecessor = index > 0 ? commits[index - 1] : null;
            context.HasSuccessor = index < commits.Length - 1;
            var isMainlineMerge = commit.IsMergedMainline(context.Configuration);
            foreach (var item in TrunkContextPreEnricherCollection)
            {
                item.Enrich(iteration, commit, context);
            }

            if (commit.HasUnsupportedMerge
                && (commit.Configuration.Increment == IncrementStrategy.Inherit || context.SemanticVersion is null))
            {
                throw new NotSupportedException("GitVersion does not support more than one merge source in a single commit yet");
            }

            foreach (var incrementer in TrunkIncrementerCollection
                .Where(element => element.MatchPrecondition(iteration, commit, context)))
            {
                foreach (var item in incrementer.GetIncrements(iteration, commit, context))
                {
                    yield return WithMergedMainlineVersion(item, commit, isMainlineMerge, context);
                }
            }

            foreach (var item in TrunkContextPostEnricherCollection)
            {
                item.Enrich(commit, context);
            }
        }
    }

    private static IBaseVersionIncrement WithMergedMainlineVersion(IBaseVersionIncrement increment, MainlineCommit commit,
        bool isMainlineMerge, MainlineContext context)
        => increment is BaseVersionOperator operation && isMainlineMerge && context.SemanticVersion is null
            && !commit.ChildIteration!.GetEffectiveConfiguration(context.Configuration).PreventIncrementWhenBranchMerged
            ? new MergedMainlineIncrement(operation, GetMergedMainlineVersion(commit.ChildIteration, context))
            : increment;

    private static (MainlineCommit[] Commits, bool HasBoundary) GetCalculationCommits(MainlineIteration iteration,
        MainlineContext context, bool includeAncestry)
    {
        List<MainlineCommit> commits = [];
        var foundTag = false;
        foreach (var commit in (includeAncestry ? iteration.GetHistory() : iteration.Commits).Reverse())
        {
            commits.Add(commit);
            var label = context.TargetLabel ?? new EffectiveConfiguration(context.Configuration, commit.Configuration)
                .GetBranchSpecificLabel(commit.BranchName, null, context.Environment, context.CurrentCommit);
            foundTag |= commit.SemanticVersions.Any(version => version.IsMatchForBranchSpecificLabel(label));
            if (foundTag && commit.Configuration.Increment != IncrementStrategy.Inherit)
            {
                return ([.. commits.AsEnumerable().Reverse()], true);
            }
            if (commit.HasChildIteration && !commit.IsMergedMainline(context.Configuration)
                && GetCalculationCommits(commit.ChildIteration, context, false).HasBoundary)
            {
                return ([.. commits.AsEnumerable().Reverse()], true);
            }
        }
        return ([.. commits.AsEnumerable().Reverse()], false);
    }

    private sealed record MergedMainlineIncrement(BaseVersionOperator Operation, BaseVersion Version) : IBaseVersionIncrement
    {
        public string Source => Operation.Source;
        public ICommit? BaseVersionSource => Operation.BaseVersionSource;
    }

    private static BaseVersion ApplyMergedMainlineIncrement(BaseVersion receiver, MergedMainlineIncrement merged,
        SemanticVersion? baseline)
    {
        var receiverVersion = receiver.GetIncrementedVersion();
        var continuesReleasedPrerelease = baseline is { IsPreRelease: true }
            && baseline.Major == receiverVersion.Major && baseline.Minor == receiverVersion.Minor
            && baseline.Patch == receiverVersion.Patch;
        // Restart a generated unnamed counting suffix for the new count anchor.
        // A released prerelease, including a numeric tag such as 2.0.0-5, continues
        // its own sequence while the numeric release version remains unchanged.
        if (receiverVersion.PreReleaseTag.Name.IsNullOrEmpty() && !continuesReleasedPrerelease)
        {
            receiverVersion = new SemanticVersion(receiverVersion) { PreReleaseTag = SemanticVersionPreReleaseTag.Empty };
        }
        return new BaseVersion(receiver.Source, receiverVersion, receiver.BaseVersionSource)
        {
            SemVerSource = receiver.GetSemVerSource()
        }.Apply(WithMainlineVersion(merged.Operation,
            merged.Version.GetIncrementedVersion(), merged.Version.GetSemVerSource()));
    }

    private static BaseVersionOperator WithMainlineVersion(BaseVersionOperator operation, SemanticVersion version,
        SemanticVersionSource? source)
        => operation.AlternativeSemanticVersion is { } alternative && !alternative.IsLessThan(version, includePreRelease: false)
            ? operation
            : operation with { AlternativeSemanticVersion = version, AlternativeSemVerSource = source };
}
