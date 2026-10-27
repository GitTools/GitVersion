using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class MainlineIterationBuilder(
    Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore,
    ITaggedSemanticVersionService taggedSemanticVersionService,
    IEnvironment environment)
{
    private static readonly IIgnoreConfiguration NoIgnoredCommits = new UnfilteredConfiguration();
    private int iterationCounter;
    private readonly HashSet<ICommit> recordedCommits = [];
    private readonly HashSet<ICommit> unresolvedBranchPoints = [];
    private readonly Dictionary<string, Dictionary<ICommit, List<(IBranch, IBranchConfiguration)>>> commitsWasBranchedFromCache = new();

    private GitVersionContext Context => contextLazy.Value;

    internal MainlineIteration BuildIteration(IBranch branch, string? targetLabel)
    {
        this.recordedCommits.Clear();
        this.unresolvedBranchPoints.Clear();
        var commitsInReverseOrder = Context.CurrentBranchCommits;
        var branchConfiguration = Context.Configuration.GetBranchConfiguration(Context.CurrentBranch);
        var iteration = CreateIteration(
            branchName: Context.CurrentBranch.Name,
            configuration: branchConfiguration
        );

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
        var taggedSemanticVersions = taggedSemanticVersionService.GetTaggedSemanticVersions(
            branch: Context.CurrentBranch,
            configuration: Context.Configuration,
            label: null,
            notOlderThan: Context.CurrentCommit.When,
            taggedSemanticVersion: taggedSemanticVersion
        );

        IterateOverCommitsRecursive(
            commitsInReverseOrder: commitsInReverseOrder,
            iteration: iteration,
            targetBranch: branch,
            targetLabel: targetLabel,
            taggedSemanticVersions: taggedSemanticVersions
        );
        return iteration;
    }

    private MainlineIteration CreateIteration(
        ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration = null, ICommit? branchedFrom = null, ICommit? branchedTo = null)
    {
        if (branchedFrom is not null && !this.recordedCommits.Contains(branchedFrom))
        {
            this.unresolvedBranchPoints.Add(branchedFrom);
        }
        var iterationCount = Interlocked.Increment(ref this.iterationCounter);
        return new MainlineIteration(
            id: $"#{iterationCount}",
            branchName: branchName,
            configuration: configuration,
            parentIteration: parentIteration,
            branchedFrom: branchedFrom,
            branchedTo: branchedTo
        );
    }

    private void IterateOverCommitsRecursive(
        IEnumerable<ICommit> commitsInReverseOrder, MainlineIteration iteration, IBranch targetBranch, string? targetLabel,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions)
    {
        // The root contains every reachable commit; keep its release-tag shortcut lazy.
        IReadOnlySet<ICommit>? availableCommits = iteration.ParentIteration is null
            ? null : commitsInReverseOrder.ToHashSet();

        var branch = repositoryStore.FindBranch(iteration.BranchName);
        var currentBranch = branch;
        TraversalState state = new(
            configuration: iteration.Configuration,
            branchName: iteration.BranchName,
            branch: branch,
            taggedSemanticVersions: taggedSemanticVersions,
            commitsWasBranchedFrom: currentBranch is null
                ? [] : GetCommitsWasBranchedFrom(currentBranch)
        );

        foreach (var item in commitsInReverseOrder)
        {
            if (!this.recordedCommits.Add(item))
            {
                continue;
            }
            this.unresolvedBranchPoints.Remove(item);

            ApplyBranchedFromTransition(item, iteration, targetBranch, state);

            var (commit, stop) = ProcessCommit(item, iteration, targetLabel, state);
            // A child can stop at its release while its enclosing traversal records
            // branch points outside the child's source-only range.
            if (stop && (availableCommits is null
                ? this.unresolvedBranchPoints.Count == 0
                : !this.unresolvedBranchPoints.Any(availableCommits.Contains)))
            {
                return;
            }

            if (item.IsMergeCommit)
            {
                HandleMergeCommit(
                    item, commit, iteration, targetBranch, targetLabel, state, availableCommits
                );
            }
        }
    }

    private void ApplyBranchedFromTransition(
        ICommit item, MainlineIteration iteration, IBranch targetBranch, TraversalState state)
    {
        if (!state.CommitsWasBranchedFrom.TryGetValue(item, out var effectiveConfigurationsWasBranchedFrom))
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
        state.Branch = repositoryStore.FindBranch(state.BranchName);

        var branchPointer = state.Branch;
        IBranch[] excludeBranches = excludeBranch is null ? [] : [excludeBranch];
        state.CommitsWasBranchedFrom = branchPointer is null
            ? [] : GetCommitsWasBranchedFrom(branchPointer, excludeBranches);

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
        state.TaggedSemanticVersions = taggedSemanticVersionService.GetTaggedSemanticVersions(
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
        ).GetBranchSpecificLabel(state.BranchName, null, environment, Context.CurrentCommit);

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

    private void HandleMergeCommit(
        ICommit item, MainlineCommit commit, MainlineIteration iteration, IBranch targetBranch,
        string? targetLabel, TraversalState state, IReadOnlySet<ICommit>? availableCommits)
    {
        if (item.Parents.Count > 2)
        {
            return;
        }
        Lazy<IReadOnlyCollection<ICommit>> mergedCommitsInReverseOrderLazy = new(
            () => repositoryStore.GetCommitLog(item.Parents[0], item.Parents[1], NoIgnoredCommits)
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

        if (childConfiguration.IsMainBranch == true && state.Configuration.IsMainBranch != true)
        {
            mergedCommitsInReverseOrderLazy = new(
                () => repositoryStore.GetCommitLog(item.Parents[1], item.Parents[0], NoIgnoredCommits)
            );
            childConfiguration = state.Configuration;
            childBranchName = iteration.BranchName;
        }

        // A merge can have no source-only history, particularly after swapping
        // a mainline source with an unchanged receiver. There is no child to build.
        if (mergedCommitsInReverseOrderLazy.Value.Count == 0)
        {
            return;
        }

        var branchedFrom = mergedCommitsInReverseOrderLazy.Value
            .LastOrDefault()?.Parents.FirstOrDefault();
        var childIteration = CreateIteration(
            branchName: childBranchName,
            configuration: childConfiguration,
            parentIteration: iteration,
            branchedFrom: branchedFrom,
            branchedTo: commit.Value
        );

        IterateOverCommitsRecursive(
            commitsInReverseOrder: mergedCommitsInReverseOrderLazy.Value.Where(source => availableCommits?.Contains(source) ?? true),
            iteration: childIteration,
            targetBranch: targetBranch,
            targetLabel: childConfiguration.IsMainBranch == true ? null : targetLabel,
            taggedSemanticVersions: state.TaggedSemanticVersions);

        commit.AddChildIteration(childIteration);
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

    // Construction retains Git topology; commit ignore rules belong to calculation.
    private sealed class UnfilteredConfiguration : IIgnoreConfiguration
    {
        private static readonly IReadOnlySet<string> Empty = new HashSet<string>();
        public DateTimeOffset? Before => null;
        public IReadOnlySet<string> Branches => Empty;
        public IReadOnlySet<string> Paths => Empty;
        public IReadOnlySet<string> Shas => Empty;
        public IReadOnlySet<string> Tags => Empty;
        public bool IsEmpty => true;
    }

    private sealed class TraversalState(
        IBranchConfiguration configuration,
        ReferenceName branchName,
        IBranch? branch,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions,
        IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>> commitsWasBranchedFrom)
    {
        public IBranchConfiguration Configuration { get; set; } = configuration;
        public ReferenceName BranchName { get; set; } = branchName;
        public IBranch? Branch { get; set; } = branch;
        public ILookup<ICommit, SemanticVersionWithTag> TaggedSemanticVersions { get; set; } = taggedSemanticVersions;
        public IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>> CommitsWasBranchedFrom { get; set; } = commitsWasBranchedFrom;
        public bool ReturnTrueWhenTheIncrementIsKnown { get; set; }
    }
}
