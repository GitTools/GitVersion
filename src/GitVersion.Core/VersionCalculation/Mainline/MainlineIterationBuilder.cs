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
    private volatile int iterationCounter;
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly ITaggedSemanticVersionService taggedSemanticVersionService = taggedSemanticVersionService.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IEnvironment environment = environment.NotNull();
    private readonly Dictionary<string, Dictionary<ICommit, List<(IBranch, IBranchConfiguration)>>> commitsWasBranchedFromCache = new();
    private readonly Dictionary<ICommit, MainlineCommit> recordedCommits = new();
    private readonly Dictionary<ICommit, MainlineIteration> introducedCommitOwners = new();

    private readonly Dictionary<MainlineIteration, Func<bool>> capturePrevious = [];
    private readonly Dictionary<MainlineCommit, Func<MainlineIteration?>> pendingMerges = [];
    private readonly Dictionary<MainlineIteration, ICommit> sourceTips = [];
    private readonly Dictionary<(MainlineIteration Iteration, string? Label, bool IncludeAncestry), bool> completedHistory = [];
    private readonly List<TraversalState> traversalStates = [];

    private GitVersionContext Context => this.contextLazy.Value;

    internal MainlineIteration BuildIteration(IBranch targetBranch, string? targetLabel)
    {
        try
        {
            return BuildIterationCore(targetBranch, targetLabel);
        }
        finally
        {
            foreach (var state in this.traversalStates)
            {
                state.Commits.Dispose();
            }
            this.capturePrevious.Clear();
            this.pendingMerges.Clear();
            this.sourceTips.Clear();
            this.completedHistory.Clear();
            this.traversalStates.Clear();
        }
    }

    private MainlineIteration BuildIterationCore(IBranch targetBranch, string? targetLabel)
    {
        this.recordedCommits.Clear();
        this.introducedCommitOwners.Clear();
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
        IterateOverCommitsRecursive(
            commitsInReverseOrder: commitsInReverseOrder,
            iteration: iteration,
            targetBranch: targetBranch,
            taggedSemanticVersions: taggedSemanticVersions,
            targetLabel: targetLabel
        );

        CompleteHistory(iteration, targetLabel, includeAncestry: false);
        return iteration;
    }

    private MainlineCommit? ResolveBranchPoint(MainlineIteration child, ICommit tip)
    {
        for (var ancestor = tip; ancestor is not null; ancestor = ancestor.Parents.FirstOrDefault())
        {
            if (!this.recordedCommits.ContainsKey(ancestor) && Context.Configuration.Ignore.Filter([ancestor]).Any())
            {
                CaptureAncestor(ancestor, child);
            }
            if (this.recordedCommits.TryGetValue(ancestor, out var recorded) && !ReferenceEquals(recorded.Iteration, child))
            {
                return recorded;
            }
        }
        return null;
    }

    private void CaptureAncestor(ICommit ancestor, MainlineIteration child)
    {
        while (!this.recordedCommits.ContainsKey(ancestor))
        {
            // The shared commit may belong to an older merged branch whose
            // own recording stopped at a release. Resume its owner, preserving
            // the nearest branch point instead of jumping to an older tag.
            var owner = this.introducedCommitOwners.GetValueOrDefault(ancestor) ?? child.ParentIteration;
            while (owner is not null && !this.capturePrevious.ContainsKey(owner))
            {
                owner = owner.ParentIteration;
            }
            if (owner is null || !this.capturePrevious[owner]())
            {
                return;
            }
        }
    }

    private MainlineIteration CreateIteration(
        ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration = null, ICommit? branchedFrom = null, ICommit? branchedTo = null)
    {
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
        IEnumerable<ICommit> commitsInReverseOrder, MainlineIteration iteration, IBranch targetBranch,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions, string? targetLabel,
        HashSet<ICommit>? traversedCommits = null)
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
            traversedCommits: traversedCommits,
            commits: commitsInReverseOrder.GetEnumerator()
        );

        this.traversalStates.Add(state);
        this.capturePrevious[iteration] = () => CapturePreviousCommit(iteration, targetBranch, state, targetLabel);
        var foundTag = false;
        while (CapturePreviousCommit(iteration, targetBranch, state, targetLabel))
        {
            var commit = iteration.Commits.First();
            var label = targetLabel ?? new EffectiveConfiguration(Context.Configuration, commit.Configuration)
                .GetBranchSpecificLabel(commit.BranchName, null, this.environment, Context.CurrentCommit);
            foundTag |= commit.SemanticVersions.Any(version => version.IsMatchForBranchSpecificLabel(label));
            if (foundTag && commit.Configuration.Increment != IncrementStrategy.Inherit)
            {
                break;
            }
        }
        // Record the receiver first so nested merges reference its history
        // instead of taking ownership of commits on its ancestry.
        state.IsRecording = false;
        BuildPendingChildren(state);
    }

    private bool CapturePreviousCommit(MainlineIteration iteration, IBranch targetBranch, TraversalState state, string? targetLabel)
    {
        // Only expand a merge when walking past it. A release on the merge
        // itself is a boundary too, and does not require its source history.
        if (iteration.Commits.FirstOrDefault() is { } oldest)
        {
            EnsureChild(oldest);
        }
        while (state.Commits.MoveNext())
        {
            var item = state.Commits.Current;
            if (state.ChildCommits.Contains(item) || !state.TraversedCommits.Add(item))
            {
                continue;
            }

            ApplyBranchedFromTransition(item, iteration, targetBranch, state);
            var commit = ProcessCommit(item, iteration, state);
            if (item.IsMergeCommit)
            {
                MergeContext mergeContext = new(state.Configuration, state.BranchName, state.TaggedSemanticVersions);
                this.pendingMerges.Add(commit, () => HandleMergeCommit(item, commit, iteration, targetBranch, state, targetLabel, mergeContext));
            }
            if (!state.IsRecording)
            {
                BuildPendingChildren(state);
            }
            return true;
        }
        state.Commits.Dispose();
        this.capturePrevious.Remove(iteration);
        return false;
    }

    private static void BuildPendingChildren(TraversalState state)
    {
        while (state.PendingChildren.TryDequeue(out var buildChild))
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

    private MainlineIteration? HandleMergeCommit(
        ICommit item, MainlineCommit commit, MainlineIteration iteration, IBranch targetBranch,
        TraversalState state, string? targetLabel, MergeContext mergeContext)
    {
        Lazy<IReadOnlyCollection<ICommit>> mergedCommitsInReverseOrderLazy = new(
            () => this.repositoryStore.GetCommitLog(item.Parents[0], item.Parents[1], Context.Configuration.Ignore)
        );

        if ((mergeContext.Configuration.TrackMergeMessage ?? Context.Configuration.TrackMergeMessage) != true
            || !MergeMessage.TryParse(item, Context.Configuration, out var mergeMessage))
        {
            return null;
        }

        if (mergeMessage.MergedBranch is null || mergeMessage.MergedBranch.EquivalentTo(mergeContext.BranchName.WithoutOrigin))
        {
            return null;
        }

        var childConfiguration = Context.Configuration.GetBranchConfiguration(mergeMessage.MergedBranch);
        var childBranchName = mergeMessage.MergedBranch;

        if (item.Parents.Count > 2)
        {
            // Report unsupported history only if calculation reaches this merge;
            // an eligible release tag can make older merges irrelevant.
            commit.HasUnsupportedMerge = true;
            return null;
        }
        var sourceTip = item.Parents[1];
        if (childConfiguration.IsMainBranch == true && mergeContext.Configuration.IsMainBranch != true)
        {
            mergedCommitsInReverseOrderLazy = new(
                () => this.repositoryStore.GetCommitLog(item.Parents[1], item.Parents[0], Context.Configuration.Ignore)
            );
            childConfiguration = mergeContext.Configuration;
            childBranchName = iteration.BranchName;
            sourceTip = item.Parents[0];
        }

        var childIteration = CreateIteration(
            branchName: childBranchName,
            configuration: childConfiguration,
            parentIteration: iteration,
            branchedFrom: FindBranchPoint(sourceTip, mergedCommitsInReverseOrderLazy.Value),
            branchedTo: item
        );

        this.sourceTips.Add(childIteration, sourceTip);
        var tags = mergeContext.TaggedSemanticVersions;
        void BuildChild() => IterateOverCommitsRecursive(
                commitsInReverseOrder: mergedCommitsInReverseOrderLazy.Value,
                iteration: childIteration,
                targetBranch: targetBranch,
                taggedSemanticVersions: tags,
                targetLabel: targetLabel,
                traversedCommits: state.TraversedCommits);

        state.ChildCommits.AddRange(mergedCommitsInReverseOrderLazy.Value);
        foreach (var introduced in mergedCommitsInReverseOrderLazy.Value.Where(introduced => !this.recordedCommits.ContainsKey(introduced)))
        {
            this.introducedCommitOwners[introduced] = childIteration;
        }
        if (state.IsRecording)
        {
            state.PendingChildren.Enqueue(BuildChild);
        }
        else
        {
            BuildChild();
        }
        return childIteration;
    }

    private void EnsureChild(MainlineCommit commit)
    {
        if (this.pendingMerges.Remove(commit, out var build) && build() is { } child)
        {
            commit.AddChildIteration(child);
        }
    }

    private ICommit? FindBranchPoint(ICommit tip, IReadOnlyCollection<ICommit> introduced)
    {
        var range = introduced.ToHashSet();
        for (var ancestor = tip; ancestor is not null; ancestor = ancestor.Parents.FirstOrDefault())
        {
            if (Context.Configuration.Ignore.Filter([ancestor]).Any() && !range.Contains(ancestor))
            {
                return ancestor;
            }
        }
        return null;
    }

    private IEnumerable<MainlineCommit> GetHistoryInReverse(MainlineIteration iteration, bool includeAncestry, MainlineCommit? tip = null)
    {
        var commit = tip ?? iteration.Commits.LastOrDefault();
        while (commit is not null)
        {
            yield return commit;
            if (commit.Predecessor is null && this.capturePrevious.TryGetValue(iteration, out var capture))
            {
                capture();
            }
            commit = commit.Predecessor;
        }
        if (!includeAncestry)
        {
            yield break;
        }
        if (this.sourceTips.TryGetValue(iteration, out var sourceTip))
        {
            iteration.BranchedFrom = ResolveBranchPoint(iteration, sourceTip)?.Value;
        }
        if (iteration.GetBranchedFrom() is { } branchPoint)
        {
            foreach (var ancestor in GetHistoryInReverse(branchPoint.Iteration, true, branchPoint))
            {
                yield return ancestor;
            }
        }
    }

    // Select structural history using tags and raw branch configuration only.
    // Message increments and numeric versions belong exclusively to calculation.
    private bool CompleteHistory(MainlineIteration iteration, string? targetLabel, bool includeAncestry)
    {
        var key = (iteration, targetLabel, includeAncestry);
        if (!this.completedHistory.TryGetValue(key, out var hasBoundary))
        {
            hasBoundary = CompleteHistoryCore(iteration, targetLabel, includeAncestry);
            this.completedHistory.Add(key, hasBoundary);
        }
        return hasBoundary;
    }

    private bool CompleteHistoryCore(MainlineIteration iteration, string? targetLabel, bool includeAncestry)
    {
        var foundTag = false;
        foreach (var commit in GetHistoryInReverse(iteration, includeAncestry))
        {
            var label = targetLabel ?? new EffectiveConfiguration(Context.Configuration, commit.Configuration)
                .GetBranchSpecificLabel(commit.BranchName, null, this.environment, Context.CurrentCommit);
            foundTag |= commit.SemanticVersions.Any(version => version.IsMatchForBranchSpecificLabel(label));
            if (foundTag && commit.Configuration.Increment != IncrementStrategy.Inherit)
            {
                return true;
            }
            EnsureChild(commit);
            if (CompleteChildHistory(commit, targetLabel))
            {
                return true;
            }
        }
        return false;
    }

    private bool CompleteChildHistory(MainlineCommit commit, string? targetLabel)
    {
        if (commit.ChildIteration is not { } child)
        {
            return false;
        }
        if (child.NumberOfCommits == 0 && this.sourceTips.TryGetValue(child, out var sharedTip))
        {
            child.BranchedFrom = ResolveBranchPoint(child, sharedTip)?.Value;
        }
        if (!commit.IsMergedMainline(Context.Configuration))
        {
            return CompleteHistory(child, targetLabel, false);
        }
        CompleteHistory(child, targetLabel, false);
        var configuration = child.GetEffectiveConfiguration(Context.Configuration);
        if (!configuration.PreventIncrementWhenBranchMerged)
        {
            var sourceTip = child.BranchedTo!.Parents[1];
            var sourceLabel = configuration.GetBranchSpecificLabel(child.BranchName, null, this.environment, sourceTip);
            CompleteHistory(child, sourceLabel, true);
        }
        return false;
    }

    private sealed class TraversalState(
        IBranchConfiguration configuration,
        ReferenceName branchName,
        IBranch? branch,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions,
        Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> commitsWasBranchedFromLazy,
        HashSet<ICommit> traversedCommits,
        IEnumerator<ICommit> commits)
    {
        public IBranchConfiguration Configuration { get; set; } = configuration;
        public ReferenceName BranchName { get; set; } = branchName;
        public IBranch? Branch { get; set; } = branch;
        public ILookup<ICommit, SemanticVersionWithTag> TaggedSemanticVersions { get; set; } = taggedSemanticVersions;
        public HashSet<ICommit> TraversedCommits { get; } = traversedCommits;
        public IEnumerator<ICommit> Commits { get; } = commits;
        public HashSet<ICommit> ChildCommits { get; } = [];
        public Queue<Action> PendingChildren { get; } = new();
        public bool IsRecording { get; set; } = true;
        public Lazy<IReadOnlyDictionary<ICommit, List<(IBranch Branch, IBranchConfiguration Value)>>> CommitsWasBranchedFromLazy { get; set; } = commitsWasBranchedFromLazy;
    }

    private sealed record MergeContext(IBranchConfiguration Configuration, ReferenceName BranchName,
        ILookup<ICommit, SemanticVersionWithTag> TaggedSemanticVersions);

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

}
