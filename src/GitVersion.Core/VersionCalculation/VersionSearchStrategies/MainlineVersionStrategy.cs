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
        var targetLabel = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null, this.environment, Context.CurrentCommit);
        IterateOverCommitsRecursive(
            commitsInReverseOrder: commitsInReverseOrder,
            iteration: iteration,
            targetBranch: configuration.Branch,
            targetLabel: targetLabel,
            taggedSemanticVersions: taggedSemanticVersions
        );

        yield return DetermineBaseVersion(iteration, targetLabel, this.incrementStrategyFinder, Context.Configuration, this.environment, Context.CurrentCommit);
    }

    private MainlineIteration CreateIteration(
        ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration = null, MainlineCommit? parentCommit = null,
        IReadOnlySet<ICommit>? includedCommits = null)
    {
        var iterationCount = Interlocked.Increment(ref this.iterationCounter);
        return new MainlineIteration(
            id: $"#{iterationCount}",
            branchName: branchName,
            configuration: configuration,
            parentIteration: parentIteration,
            parentCommit: parentCommit,
            includedCommits: includedCommits
        );
    }

    private bool IterateOverCommitsRecursive(
        IEnumerable<ICommit> commitsInReverseOrder, MainlineIteration iteration, IBranch targetBranch, string? targetLabel,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions, HashSet<ICommit>? traversedCommits = null,
        bool stopOnMatchingTag = true)
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
            stopOnMatchingTag: stopOnMatchingTag
        );

        foreach (var item in commitsInReverseOrder)
        {
            if (iteration.IncludedCommits is { } includedCommits && !includedCommits.Contains(item))
            {
                continue;
            }

            if (!traversedCommits.Add(item))
            {
                continue;
            }

            ApplyBranchedFromTransition(item, iteration, targetBranch, state);

            var (commit, stop) = ProcessCommit(item, iteration, targetLabel, state);
            if (stop && stopOnMatchingTag)
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
        ).GetBranchSpecificLabel(state.BranchName, null, this.environment, Context.CurrentCommit);

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
            () => GetMergedCommitsInReverseOrder(item, iteration, 1)
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

        if (childConfiguration.IsMainBranch == true && state.Configuration.IsMainBranch == true)
        {
            if (IsConfiguredSupportMerge(state.BranchName, childBranchName))
            {
                // A linear slice after the merge base can miss older, newly merged
                // support commits when main has already been merged into support.
                mergedCommitsInReverseOrderLazy = new(() => this.repositoryStore.GetCommitLog(
                    item.Parents[0], item.Parents.Skip(1).Single(), Context.Configuration.Ignore));
                // Validate nested merge edges using the existing traversal, without contributing
                // a child iteration to main's version or marking its history as traversed yet.
                IterateOverCommitsRecursive(
                    commitsInReverseOrder: mergedCommitsInReverseOrderLazy.Value,
                    iteration: CreateIteration(childBranchName, childConfiguration),
                    targetBranch: targetBranch,
                    targetLabel: targetLabel,
                    taggedSemanticVersions: state.TaggedSemanticVersions,
                    stopOnMatchingTag: false);
                SetMergedSupportIteration(mergedCommitsInReverseOrderLazy.Value, commit, childConfiguration,
                    childBranchName, targetBranch, targetLabel, state);
                traversedCommits.AddRange(mergedCommitsInReverseOrderLazy.Value);
                return false;
            }

            if (!IsConfiguredSupportMerge(childBranchName, state.BranchName))
            {
                throw new NotImplementedException();
            }

            // Main merged into support uses the existing recursive calculation, including
            // main's tagged baseline and the receiver/source increment controls.
        }
        else if (childConfiguration.IsMainBranch == true)
        {
            mergedCommitsInReverseOrderLazy = new(
                () => GetMergedCommitsInReverseOrder(item, iteration, 0)
            );
            childConfiguration = state.Configuration;
            childBranchName = iteration.BranchName;
        }

        var childIteration = CreateIteration(
            branchName: childBranchName,
            configuration: childConfiguration,
            parentIteration: iteration,
            parentCommit: commit,
            includedCommits: iteration.IncludedCommits
        );

        var done = IterateOverCommitsRecursive(
            commitsInReverseOrder: mergedCommitsInReverseOrderLazy.Value,
            iteration: childIteration,
            targetBranch: targetBranch,
            targetLabel: targetLabel,
            taggedSemanticVersions: state.TaggedSemanticVersions,
            traversedCommits: traversedCommits,
            stopOnMatchingTag: state.StopOnMatchingTag);

        commit.AddChildIteration(childIteration);
        if (iteration.IncludedCommits is not null && childIteration.Commits.Count == 0)
        {
            // This merge only brings history already present on the receiving main.
            commit.MergedBranchIncrement = VersionField.None;
        }
        if (done)
        {
            return true;
        }

        traversedCommits.AddRange(mergedCommitsInReverseOrderLazy.Value);
        return false;
    }

    private IReadOnlyCollection<ICommit> GetMergedCommitsInReverseOrder(
        ICommit commit, MainlineIteration iteration, int parentIndex)
    {
        if (iteration.IncludedCommits is null)
        {
            return [.. this.incrementStrategyFinder.GetMergedCommits(commit, parentIndex, Context.Configuration.Ignore).Reverse()];
        }

        var mergedHead = commit.Parents.Skip(1).Single();
        return parentIndex == 1
            ? this.repositoryStore.GetCommitLog(commit.Parents[0], mergedHead, Context.Configuration.Ignore)
            : this.repositoryStore.GetCommitLog(mergedHead, commit.Parents[0], Context.Configuration.Ignore);
    }

    private void SetMergedSupportIteration(
        IReadOnlyCollection<ICommit> mergedCommits, MainlineCommit commit, IBranchConfiguration childConfiguration,
        ReferenceName childBranchName, IBranch targetBranch, string? targetLabel, TraversalState state)
    {
        // Keep traversing main for its baseline, but retain support's ordered mainline
        // operations. Its operands become numeric floors rather than replacing main.
        var supportIteration = CreateIteration(childBranchName, childConfiguration, commit.Iteration, commit,
            includedCommits: mergedCommits.ToHashSet());
        IterateOverCommitsRecursive(mergedCommits, supportIteration, targetBranch, targetLabel,
            state.TaggedSemanticVersions, stopOnMatchingTag: state.StopOnMatchingTag);
        commit.MergedSupportIteration = supportIteration;
        var mainTip = commit.Value.Parents[0];
        var supportTip = commit.Value.Parents.Skip(1).Single();
        if (this.repositoryStore.FindMergeBase(mainTip, supportTip)?.Equals(mainTip) != true)
        {
            // Build both histories during traversal. Version calculation only consumes
            // these completed iterations; it never changes their commit selection.
            var independentIteration = CreateIteration(childBranchName, childConfiguration, commit.Iteration, commit);
            IterateOverCommitsRecursive(this.repositoryStore.GetCommitLog(null, supportTip, Context.Configuration.Ignore),
                independentIteration, targetBranch, targetLabel, state.TaggedSemanticVersions);
            commit.MergedSupportVersion = new(() => GetSupportVersion(independentIteration, supportTip, targetLabel));
        }
    }

    private (BaseVersion Version, SemanticVersion Baseline) GetSupportVersion(
        MainlineIteration iteration, ICommit tip, string? targetLabel)
    {
        var steps = GetIncrements(iteration, targetLabel, this.incrementStrategyFinder,
            Context.Configuration, this.environment, Context.CurrentCommit).ToArray();
        var version = ApplyIncrements(steps);
        var baseline = steps.OfType<BaseVersionOperand>().FirstOrDefault()?.SemanticVersion ?? SemanticVersion.Empty;
        // A released source tip contributes its tag, not its next pending increment.
        if (version.Operand.BaseVersionSource?.Equals(tip) == true
            && iteration.GetEffectiveConfiguration(Context.Configuration).PreventIncrementWhenCurrentCommitTagged
            && version.Operator is { } operation)
        {
            version = version with { Operator = operation with { Increment = VersionField.None } };
        }
        return (version, baseline);
    }

    // These are configuration role keys, not literal Git branch names. The enclosing
    // merge handler also requires both matched branches to be main branches.
    private bool IsConfiguredSupportMerge(ReferenceName targetBranch, ReferenceName sourceBranch)
        => Context.Configuration.Branches.TryGetValue(ConfigurationConstants.MainBranchKey, out var mainConfiguration)
            && mainConfiguration.IsMatch(targetBranch.WithoutOrigin)
            && Context.Configuration.Branches.TryGetValue(ConfigurationConstants.SupportBranchKey, out var supportConfiguration)
            && supportConfiguration.IsMatch(sourceBranch.WithoutOrigin);

    private sealed class TraversalState(
        IBranchConfiguration configuration,
        ReferenceName branchName,
        IBranch? branch,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions,
        Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> commitsWasBranchedFromLazy,
        bool stopOnMatchingTag)
    {
        public IBranchConfiguration Configuration { get; set; } = configuration;
        public ReferenceName BranchName { get; set; } = branchName;
        public IBranch? Branch { get; set; } = branch;
        public ILookup<ICommit, SemanticVersionWithTag> TaggedSemanticVersions { get; set; } = taggedSemanticVersions;
        public Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> CommitsWasBranchedFromLazy { get; set; } = commitsWasBranchedFromLazy;
        public bool ReturnTrueWhenTheIncrementIsKnown { get; set; }
        public bool StopOnMatchingTag { get; } = stopOnMatchingTag;
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
        IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
    {
        iteration.NotNull();

        var incrementSteps = GetIncrements(iteration, targetLabel, incrementStrategyFinder, configuration, environment, currentCommit).ToArray();
        return ApplyIncrements(incrementSteps);
    }

    private static BaseVersion ApplyIncrements(IEnumerable<IBaseVersionIncrement> incrementSteps)
    {
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
                case MergedSupportIncrement support:
                    result = ApplyMergedSupportIncrement(result ?? new BaseVersion(), support);
                    break;
                case MergedSupportVersionFloor floor:
                    if (result is null || result.GetIncrementedVersion().IsLessThan(floor.Operand.SemanticVersion, includePreRelease: false))
                    {
                        result = new BaseVersion(floor.Operand with { BaseVersionSource = floor.BaseVersionSource });
                    }
                    break;
            }
        }
        return result ?? throw new InvalidOperationException();
    }

    private static IEnumerable<IBaseVersionIncrement> GetIncrements(MainlineIteration iteration, string? targetLabel,
        IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
    {
        MainlineContext context = new(incrementStrategyFinder, configuration, environment, currentCommit)
        {
            TargetLabel = targetLabel
        };

        foreach (var commit in iteration.Commits)
        {
            var supportIncrements = GetMergedSupportIncrements(commit, context);
            foreach (var item in TrunkContextPreEnricherCollection)
            {
                item.Enrich(iteration, commit, context);
            }

            foreach (var incrementer in TrunkIncrementerCollection
                .Where(element => element.MatchPrecondition(iteration, commit, context)))
            {
                foreach (var item in incrementer.GetIncrements(iteration, commit, context))
                {
                    yield return item is BaseVersionOperator operation && supportIncrements.Count != 0
                        ? new MergedSupportIncrement(operation, supportIncrements, commit.MergedSupportVersion)
                        : item;
                }
            }

            foreach (var item in TrunkContextPostEnricherCollection)
            {
                item.Enrich(commit, context);
            }
        }
    }

    private sealed record MergedSupportIncrement(BaseVersionOperator Operation,
        IReadOnlyList<IBaseVersionIncrement> Steps, Lazy<(BaseVersion Version, SemanticVersion Baseline)>? Version) : IBaseVersionIncrement
    {
        public string Source => Operation.Source;
        public ICommit? BaseVersionSource => Operation.BaseVersionSource;
    }

    private static BaseVersion ApplyMergedSupportIncrement(BaseVersion main, MergedSupportIncrement support)
    {
        var mainVersion = main.GetIncrementedVersion();
        if (support.Version?.Value is { } independent
            && IsSameOrNewerReleaseLine(independent.Baseline, mainVersion))
        {
            // Compare independently calculated progress on the same (or a newer) line.
            // Keep main's merge operator and counting anchor; only add a numeric floor.
            return main.Apply(WithSupportVersion(support.Operation,
                independent.Version.GetIncrementedVersion(), independent.Version.GetSemVerSource()));
        }

        // An older maintenance line still transfers its ordered increments to main's
        // newer release line, without replacing main's baseline with the older version.
        foreach (var step in support.Steps.SkipLast(1))
        {
            main = ApplySupportStep(main, step, support.Operation);
        }
        var operation = support.Operation;
        if (support.Steps.LastOrDefault() is BaseVersionOperator { AlternativeSemanticVersion: { } alternative } last)
        {
            operation = WithSupportVersion(operation, alternative, last.AlternativeSemVerSource);
        }
        return main.Apply(operation);
    }

    private static bool IsSameOrNewerReleaseLine(SemanticVersion source, SemanticVersion main)
        => source.Major > main.Major || source.Major == main.Major && source.Minor >= main.Minor;

    private static BaseVersionOperator WithSupportVersion(BaseVersionOperator operation, SemanticVersion version,
        SemanticVersionSource? source)
        => operation.AlternativeSemanticVersion is { } alternative && !alternative.IsLessThan(version, includePreRelease: false)
            ? operation
            : operation with { AlternativeSemanticVersion = version, AlternativeSemVerSource = source };

    private static BaseVersion ApplySupportStep(BaseVersion version, IBaseVersionIncrement step, BaseVersionOperator merge)
        => step switch
        {
            BaseVersionOperator operation => version.Apply(operation with { BaseVersionSource = merge.BaseVersionSource, Label = merge.Label }),
            MergedSupportVersionFloor floor when version.GetIncrementedVersion().IsLessThan(floor.Operand.SemanticVersion, includePreRelease: false)
                => new BaseVersion(floor.Operand with { BaseVersionSource = merge.BaseVersionSource }),
            MergedSupportVersionFloor => version,
            _ => throw new InvalidOperationException("Unexpected merged support version step.")
        };

    private sealed record MergedSupportVersionFloor(BaseVersionOperand Operand, ICommit? BaseVersionSource = null) : IBaseVersionIncrement
    {
        public string Source => Operand.Source;
    }

    private static IReadOnlyList<IBaseVersionIncrement> GetMergedSupportIncrements(MainlineCommit commit, MainlineContext context)
    {
        if (commit.MergedSupportIteration is not { } supportIteration)
        {
            return [];
        }

        commit.MergedBranchIncrement = VersionField.None;
        if (supportIteration.GetEffectiveConfiguration(context.Configuration).PreventIncrementWhenBranchMerged)
        {
            return [];
        }

        var steps = GetIncrements(supportIteration, context.TargetLabel, context.IncrementStrategyFinder,
            context.Configuration, context.Environment, context.CurrentCommit).ToArray();
        var increments = steps.Select(step => step is BaseVersionOperand operand
            ? new MergedSupportVersionFloor(operand)
            : step).ToList();

        // A tag on the source tip is already released: consolidate its pending merge
        // increment against main, then apply the tag floor. A tag followed by more
        // source commits must establish the floor before those increments instead.
        if (steps is [BaseVersionOperand operand, BaseVersionOperator operation]
            && operand.BaseVersionSource?.Equals(supportIteration.Commits.Last().Value) == true)
        {
            increments = [operation.AlternativeSemanticVersion is { } alternative
                && !alternative.IsLessThan(operand.SemanticVersion, includePreRelease: false)
                    ? operation
                    : operation with
                    {
                        AlternativeSemanticVersion = operand.SemanticVersion,
                        AlternativeSemVerSource = operand.SemVerSource
                            ?? new SemanticVersionSource(operand.Source, operand.SemanticVersion, operand.BaseVersionSource, VersionField.None)
                    }];
        }

        commit.MergedBranchIncrement = (increments.LastOrDefault() as BaseVersionOperator)?.Increment ?? VersionField.None;
        return increments;
    }
}
