using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal class IncrementStrategyFinder(
    Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore,
    ITaggedSemanticVersionRepository taggedSemanticVersionRepository,
    IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder,
    IEnvironment environment)
    : IIncrementStrategyFinder
{
    private readonly Dictionary<CommitIncrementCacheKey, CommitMessageIncrement?> commitIncrementCache = [];
    private readonly Dictionary<(string Commit, bool IsMergedTipBaseVersionSource, EffectiveConfiguration Target),
        MergedBranchIncrement[]> mergedBranchIncrementCache = [];
    private readonly Dictionary<(string Branch, string? Tip), EffectiveConfiguration[]> effectiveConfigurationCache = [];
    private readonly Dictionary<string, HashSet<string>> firstParentHistoryCache = [];
    private readonly Dictionary<string, Dictionary<string, int>> headCommitsMapCache = [];
    private readonly Dictionary<string, ICommit[]> headCommitsCache = [];
    private readonly Dictionary<(string Commit, EffectiveConfiguration Target), bool> linearMainBranchHistoryCache = [];

    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();
    private readonly IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder = effectiveBranchConfigurationFinder.NotNull();
    private readonly IEnvironment environment = environment.NotNull();

    private GitVersionContext Context => this.contextLazy.Value;

    public VersionField DetermineIncrementedField(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement, EffectiveConfiguration configuration, string? label)
    {
        currentCommit.NotNull();
        configuration.NotNull();

        var targetIncrement = DetermineIncrementedFieldInternal(
            currentCommit, baseVersionSource, shouldIncrement, configuration, label);

        if (!configuration.TrackMergeMessage || !HasLinearMainBranchHistory(currentCommit, configuration))
        {
            return targetIncrement.Increment;
        }

        var increments = GetIncrementsFromCommitHistory(
            currentCommit, baseVersionSource, shouldIncrement, configuration, label, targetIncrement);

        var result = VersionField.None;
        var hasIncrement = false;
        foreach (var increment in increments)
        {
            hasIncrement = true;
            result = result.Consolidate(increment.Increment);
            if (increment.VersionBumpNeedsToBeReset)
            {
                break;
            }
        }

        return hasIncrement ? result : targetIncrement.Increment;
    }

    private CommitMessageIncrement DetermineIncrementedFieldInternal(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement,
        EffectiveConfiguration configuration, string? label,
        IReadOnlySet<string>? includedCommits = null)
    {
        var commitMessageIncrement = FindCommitMessageIncrement(
            configuration, baseVersionSource, currentCommit, label, includedCommits);

        return DetermineIncrementedField(commitMessageIncrement, shouldIncrement, configuration);
    }

    private static CommitMessageIncrement DetermineIncrementedField(
        CommitMessageIncrement? commitMessageIncrement, bool shouldIncrement, EffectiveConfiguration configuration)
    {
        var defaultIncrement = configuration.Increment.ToVersionField();

        // use the default branch configuration increment strategy if there are no commit message overrides
        if (commitMessageIncrement == null)
        {
            return new(shouldIncrement ? defaultIncrement : VersionField.None, VersionBumpNeedsToBeReset: false);
        }

        // don't increment for less than the branch configuration increment, if the absence of commit messages would have
        // still resulted in an increment of configuration.Increment
        if (shouldIncrement && !commitMessageIncrement.Value.VersionBumpNeedsToBeReset
            && commitMessageIncrement.Value.Increment < defaultIncrement)
        {
            return new(defaultIncrement, VersionBumpNeedsToBeReset: false);
        }

        return commitMessageIncrement.Value;
    }

    private IEnumerable<CommitMessageIncrement> GetIncrementsFromCommitHistory(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement,
        EffectiveConfiguration targetConfiguration, string? targetLabel, CommitMessageIncrement targetIncrement)
    {
        var commitLog = this.repositoryStore
            .GetCommitLog(baseVersionSource, currentCommit, targetConfiguration.Ignore);
        var includedCommits = commitLog.Select(commit => commit.Sha).ToHashSet();
        var history = GetFirstParentCommitHistory(
            currentCommit, baseVersionSource, targetConfiguration, includedCommits).ToArray();

        if (!history.Any(item => item.MergedBranch is not null))
        {
            yield return targetIncrement;
            yield break;
        }

        var targetCommitHistory = GetCommitHistory(
                targetConfiguration.TagPrefixPattern,
                targetConfiguration.SemanticVersionFormat,
                baseVersionSource,
                currentCommit,
                targetLabel,
                targetConfiguration.Ignore)
            .Select(commit => commit.Sha)
            .ToHashSet();
        var commitOrder = commitLog
            .Select((commit, index) => (commit.Sha, Index: index))
            .ToDictionary(item => item.Sha, item => item.Index);
        var defaultTargetIncrement = DetermineIncrementedField(
            commitMessageIncrement: null, shouldIncrement, targetConfiguration).Increment;
        List<ICommit> targetSegment = [];

        foreach (var entry in history)
        {
            if (entry.MergedBranch is not { } mergedBranch)
            {
                targetSegment.AddRange(entry.TargetCommits);
                continue;
            }

            if (!targetCommitHistory.Contains(entry.Commit.Sha))
            {
                continue;
            }

            if (targetSegment.Count != 0)
            {
                yield return GetTargetIncrement(
                    targetSegment, targetCommitHistory, commitOrder, shouldIncrement, targetConfiguration);
                targetSegment.Clear();
            }

            var targetMergeMessageIncrement = FindCommitMessageIncrement(
                targetConfiguration, [entry.Commit], targetCommitHistory);
            if (targetMergeMessageIncrement is not null)
            {
                yield return DetermineIncrementedField(
                    targetMergeMessageIncrement, shouldIncrement, targetConfiguration);
            }

            var sourceIncrements = GetMergedBranchIncrements(
                entry.Commit, mergedBranch, baseVersionSource, targetConfiguration);
            if (sourceIncrements.Length != 0)
            {
                yield return ConsolidateMergedBranchIncrements(
                    sourceIncrements, targetConfiguration, defaultTargetIncrement);
            }
        }

        if (targetSegment.Count != 0)
        {
            yield return GetTargetIncrement(
                targetSegment, targetCommitHistory, commitOrder, shouldIncrement, targetConfiguration);
        }
    }

    private IEnumerable<CommitHistoryEntry> GetFirstParentCommitHistory(
        ICommit currentCommit, ICommit? baseVersionSource, EffectiveConfiguration targetConfiguration,
        IReadOnlySet<string> includedCommits)
    {
        for (ICommit? commit = currentCommit; commit is not null; commit = commit.Parents.FirstOrDefault())
        {
            if (baseVersionSource?.Equals(commit) == true)
            {
                yield break;
            }

            var isCommitIncluded = includedCommits.Contains(commit.Sha);
            ReferenceName? mergedBranch = null;
            if (isCommitIncluded
                && commit.IsMergeCommit
                && commit.Parents.Count == 2
                && MergeMessage.TryParse(commit, Context.Configuration, out var mergeMessage))
            {
                mergedBranch = mergeMessage.MergedBranch;
            }

            ICommit[] targetCommits = isCommitIncluded ? [commit] : [];
            if (commit.IsMergeCommit && mergedBranch is null)
            {
                var firstParent = commit.Parents[0];
                targetCommits =
                [
                    .. targetCommits,
                    .. commit.Parents.Skip(1)
                        .SelectMany(parent => this.repositoryStore.GetCommitLog(
                            firstParent, parent, targetConfiguration.Ignore))
                        .DistinctBy(parent => parent.Sha)
                ];
            }

            if (targetCommits.Length == 0)
            {
                continue;
            }

            yield return new(commit, mergedBranch, targetCommits);
        }
    }

    private CommitMessageIncrement GetTargetIncrement(
        IEnumerable<ICommit> targetCommits, IReadOnlySet<string> targetCommitHistory,
        IReadOnlyDictionary<string, int> commitOrder,
        bool shouldIncrement, EffectiveConfiguration targetConfiguration) =>
        DetermineIncrementedField(
            FindCommitMessageIncrement(
                targetConfiguration,
                targetCommits
                    .Where(commit => targetCommitHistory.Contains(commit.Sha))
                    .DistinctBy(commit => commit.Sha)
                    .OrderBy(commit => commitOrder[commit.Sha]),
                targetCommitHistory),
            shouldIncrement,
            targetConfiguration);

    private MergedBranchIncrement[] GetMergedBranchIncrements(
        ICommit mergeCommit, ReferenceName mergedBranch, ICommit? baseVersionSource,
        EffectiveConfiguration targetConfiguration) =>
        this.mergedBranchIncrementCache.GetOrAdd(
            (mergeCommit.Sha, mergeCommit.Parents[1].Equals(baseVersionSource), targetConfiguration), () =>
            {
                var sourceBranchConfiguration = Context.Configuration.GetBranchConfiguration(mergedBranch);
                var mergeBase = this.repositoryStore.FindMergeBase(
                    mergeCommit.Parents[0], mergeCommit.Parents[1]);

                return [.. GetSourceConfigurations(
                        mergedBranch, mergeCommit.Parents[1], sourceBranchConfiguration, targetConfiguration)
                    .Select(sourceConfiguration => GetMergedBranchIncrement(
                        mergeCommit.Parents[1], mergedBranch, baseVersionSource, mergeBase,
                        sourceBranchConfiguration, sourceConfiguration))];
            });

    private MergedBranchIncrement GetMergedBranchIncrement(
        ICommit mergedBranchTip, ReferenceName mergedBranch, ICommit? baseVersionSource, ICommit? mergeBase,
        IBranchConfiguration sourceBranchConfiguration, EffectiveConfiguration sourceConfiguration)
    {
        var sourceLabel = sourceConfiguration.GetBranchSpecificLabel(
            mergedBranch, null, this.environment);
        var preventIncrementWhenBranchMerged = sourceBranchConfiguration.PreventIncrement.WhenBranchMerged
            ?? (sourceBranchConfiguration.Increment == IncrementStrategy.Inherit
                ? sourceConfiguration.PreventIncrementWhenBranchMerged
                : Context.Configuration.PreventIncrement.WhenBranchMerged);
        var sourceIncrement = DetermineIncrementedFieldInternal(
            currentCommit: mergedBranchTip,
            baseVersionSource: mergeBase,
            shouldIncrement: ShouldIncrementTaggedCommit(
                mergedBranchTip, baseVersionSource, sourceConfiguration, sourceLabel),
            configuration: sourceConfiguration,
            label: sourceLabel
        );

        return new(sourceIncrement, preventIncrementWhenBranchMerged);
    }

    private static CommitMessageIncrement ConsolidateMergedBranchIncrements(
        IEnumerable<MergedBranchIncrement> sourceIncrements,
        EffectiveConfiguration targetConfiguration, VersionField targetIncrement)
    {
        var result = new CommitMessageIncrement(VersionField.None, VersionBumpNeedsToBeReset: false);
        foreach (var sourceIncrement in sourceIncrements)
        {
            result = result.Consolidate(SelectIncrement(
                targetConfiguration.PreventIncrementOfMergedBranch,
                sourceIncrement.PreventIncrementWhenBranchMerged,
                targetIncrement,
                sourceIncrement.Increment
            ));
        }

        return result;
    }

    private bool ShouldIncrementTaggedCommit(
        ICommit commit, ICommit? baseVersionSource, EffectiveConfiguration configuration, string? label) =>
        !commit.Equals(baseVersionSource)
        || !configuration.PreventIncrementWhenCurrentCommitTagged
        || !this.taggedSemanticVersionRepository
            .GetTaggedSemanticVersions(
                configuration.TagPrefixPattern, configuration.SemanticVersionFormat, configuration.Ignore)[commit]
            .Where(versionWithTag => versionWithTag.Tag.Commit.When <= Context.CurrentCommit.When)
            .Any(versionWithTag => versionWithTag.Value.IsMatchForBranchSpecificLabel(label));

    private IEnumerable<EffectiveConfiguration> GetSourceConfigurations(
        ReferenceName mergedBranch, ICommit mergedBranchTip,
        IBranchConfiguration sourceBranchConfiguration, EffectiveConfiguration targetConfiguration)
    {
        var existingBranch = this.repositoryStore.Branches
            .Where(candidate => candidate.Name.EquivalentTo(mergedBranch.WithoutOrigin)
                && candidate.Tip?.Equals(mergedBranchTip) == true)
            .MinBy(candidate => candidate.IsRemote);

        if (existingBranch is not null)
        {
            var configurations = GetEffectiveConfigurations(existingBranch);
            if (configurations.Length != 0)
            {
                return configurations;
            }
        }

        if (sourceBranchConfiguration.Increment != IncrementStrategy.Inherit)
        {
            return [Context.Configuration.GetEffectiveConfiguration(mergedBranch)];
        }

        var inheritedConfigurations = FindClosestSourceBranches(
                mergedBranchTip, sourceBranchConfiguration, Context.Configuration,
                this.repositoryStore)
            .SelectMany(source => GetEffectiveConfigurations(source.Branch, source.Tip))
            .Select(source => new EffectiveConfiguration(
                Context.Configuration, sourceBranchConfiguration, source))
            .Distinct()
            .ToArray();

        return inheritedConfigurations.Length != 0
            ? inheritedConfigurations
            : [Context.Configuration.GetEffectiveConfiguration(mergedBranch, targetConfiguration)];
    }

    private EffectiveConfiguration[] GetEffectiveConfigurations(IBranch branch, ICommit? tip = null)
    {
        tip ??= branch.Tip;
        return this.effectiveConfigurationCache.GetOrAdd(
            (branch.Name.ToString(), tip?.Sha),
            () => branch.Tip?.Equals(tip) == true
                ? [.. this.effectiveBranchConfigurationFinder
                    .GetConfigurations(branch, Context.Configuration)
                    .Select(configuration => configuration.Value)
                    .Distinct()]
                : [.. GetHistoricalEffectiveConfigurations(
                    branch, tip, new(StringComparer.OrdinalIgnoreCase)).Distinct()]
        );
    }

    private IEnumerable<EffectiveConfiguration> GetHistoricalEffectiveConfigurations(
        IBranch branch, ICommit? tip, HashSet<string> traversedBranches)
    {
        if (tip is null || !traversedBranches.Add(branch.Name.WithoutOrigin))
        {
            yield break;
        }

        var branchConfiguration = Context.Configuration.GetBranchConfiguration(branch.Name);
        if (branchConfiguration.Increment != IncrementStrategy.Inherit)
        {
            yield return new(Context.Configuration, branchConfiguration);
            yield break;
        }

        var sources = FindClosestSourceBranches(
                tip, branchConfiguration, Context.Configuration, this.repositoryStore)
            .ToArray();
        if (sources.Length == 0)
        {
            if (Context.Configuration.Increment != IncrementStrategy.Inherit)
            {
                yield return new(Context.Configuration, branchConfiguration);
            }
            yield break;
        }

        foreach (var source in sources)
        {
            foreach (var parentConfiguration in GetHistoricalEffectiveConfigurations(
                source.Branch, source.Tip,
                new(traversedBranches, traversedBranches.Comparer)))
            {
                yield return new(
                    Context.Configuration, branchConfiguration, parentConfiguration);
            }
        }
    }

    private IEnumerable<HistoricalSourceBranch> FindClosestSourceBranches(
        ICommit mergedBranchTip, IBranchConfiguration mergedBranchConfiguration,
        IGitVersionConfiguration configuration, IRepositoryStore repositoryStore)
    {
        var candidates = repositoryStore.Branches
            .Where(branch => (!configuration.Ignore.IsBranchIgnored(branch.Name)
                    || IsCurrentOrLinearMainBranch(branch))
                && IsConfiguredSourceBranch(branch, mergedBranchConfiguration, configuration));

        var closestDistance = int.MaxValue;
        List<HistoricalSourceBranch> result = [];
        foreach (var candidate in candidates)
        {
            if (candidate.Tip is null)
            {
                continue;
            }

            if (FindFirstParentSource(mergedBranchTip, candidate.Tip) is not { } source)
            {
                continue;
            }
            if (source.Distance < closestDistance)
            {
                closestDistance = source.Distance;
                result.Clear();
            }
            if (source.Distance == closestDistance)
            {
                result.Add(new(candidate, source.Commit));
            }
        }

        return result
            .GroupBy(candidate => candidate.Branch.Name.WithoutOrigin, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(candidate => candidate.Branch.IsRemote).First());
    }

    private bool IsCurrentOrLinearMainBranch(IBranch branch)
    {
        if (branch.Name.EquivalentTo(Context.CurrentBranch.Name.WithoutOrigin))
        {
            return true;
        }
        if (branch.Tip is not { } tip
            || !GetEffectiveConfigurations(branch).Any(configuration => configuration.IsMainBranch)
            || FindFirstParentSource(Context.CurrentCommit, tip) is not { } source)
        {
            return false;
        }
        return !ContainsMergeCommit(Context.CurrentCommit, source.Distance);
    }

    private (ICommit Commit, int Distance)? FindFirstParentSource(
        ICommit mergedBranchTip, ICommit sourceBranchTip)
    {
        var sourceHistory = this.firstParentHistoryCache.GetOrAdd(sourceBranchTip.Sha, () =>
        {
            HashSet<string> result = [];
            for (ICommit? commit = sourceBranchTip; commit is not null; commit = commit.Parents.FirstOrDefault())
            {
                result.Add(commit.Sha);
            }
            return result;
        });

        var distance = 0;
        for (ICommit? commit = mergedBranchTip; commit is not null; commit = commit.Parents.FirstOrDefault())
        {
            if (sourceHistory.Contains(commit.Sha))
            {
                return (commit, distance);
            }
            distance++;
        }
        return null;
    }

    private bool HasLinearMainBranchHistory(ICommit commit, EffectiveConfiguration targetConfiguration)
    {
        if (IsPullRequestBranch(Context.CurrentBranch, Context.Configuration))
        {
            return false;
        }

        return this.linearMainBranchHistoryCache.GetOrAdd((commit.Sha, targetConfiguration), () =>
        {
            var closestDistance = GetClosestMainBranchDistance(commit, targetConfiguration);
            return closestDistance != int.MaxValue && !ContainsMergeCommit(commit, closestDistance);
        });
    }

    private int GetClosestMainBranchDistance(ICommit commit, EffectiveConfiguration targetConfiguration)
    {
        var closestDistance = int.MaxValue;
        foreach (var branch in this.repositoryStore.Branches)
        {
            if (!IsMainBranch(branch, targetConfiguration)
                || branch.Tip is not { } tip
                || FindFirstParentSource(commit, tip) is not { } source)
            {
                continue;
            }
            closestDistance = Math.Min(closestDistance, source.Distance);
        }
        return closestDistance;
    }

    private bool IsMainBranch(IBranch branch, EffectiveConfiguration targetConfiguration) =>
        branch.Name.EquivalentTo(Context.CurrentBranch.Name.WithoutOrigin)
            ? targetConfiguration.IsMainBranch
            : GetEffectiveConfigurations(branch).Any(configuration => configuration.IsMainBranch);

    private static bool ContainsMergeCommit(ICommit commit, int distance)
    {
        for (ICommit? current = commit; distance > 0;
             current = current?.Parents.FirstOrDefault(), distance--)
        {
            if (current?.IsMergeCommit == true)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsPullRequestBranch(IBranch branch, IGitVersionConfiguration configuration) =>
        branch.Name.IsPullRequest
        || configuration.Branches.TryGetValue(ConfigurationConstants.PullRequestBranchKey, out var pullRequestConfiguration)
        && pullRequestConfiguration.IsMatch(branch.Name.WithoutOrigin);

    private static bool IsConfiguredSourceBranch(
        IBranch candidate, IBranchConfiguration mergedBranchConfiguration,
        IGitVersionConfiguration configuration) =>
        mergedBranchConfiguration.SourceBranches.Any(sourceBranch =>
            configuration.Branches.TryGetValue(sourceBranch, out var sourceBranchConfiguration)
            && sourceBranchConfiguration.IsMatch(candidate.Name.WithoutOrigin));

    private static CommitMessageIncrement SelectIncrement(
        bool preventIncrementOfMergedBranch, bool? preventIncrementWhenBranchMerged,
        VersionField targetIncrement, CommitMessageIncrement sourceIncrement)
    {
        if (preventIncrementOfMergedBranch)
        {
            return preventIncrementWhenBranchMerged == true
                ? new(targetIncrement, VersionBumpNeedsToBeReset: false)
                : sourceIncrement;
        }

        return preventIncrementWhenBranchMerged == true
            ? new(targetIncrement, VersionBumpNeedsToBeReset: false)
            : new(targetIncrement.Consolidate(sourceIncrement.Increment), sourceIncrement.VersionBumpNeedsToBeReset);
    }

    private readonly record struct MergedBranchIncrement(
        CommitMessageIncrement Increment, bool? PreventIncrementWhenBranchMerged);

    private readonly record struct HistoricalSourceBranch(IBranch Branch, ICommit Tip);

    private readonly record struct CommitHistoryEntry(
        ICommit Commit, ReferenceName? MergedBranch, IReadOnlyList<ICommit> TargetCommits);

    private readonly record struct CommitIncrementCacheKey(
        string Commit, Regex Major, Regex Minor, Regex Patch, Regex NoBump, Regex Reset);

    private CommitMessageIncrement? GetIncrementForCommits(EffectiveConfiguration configuration, ICommit[] commits)
    {
        commits.NotNull();

        var majorRegex = TryGetRegexOrDefault(configuration.MajorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMajorRegex);
        var minorRegex = TryGetRegexOrDefault(configuration.MinorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMinorRegex);
        var patchRegex = TryGetRegexOrDefault(configuration.PatchVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultPatchRegex);
        var noBumpRegex = TryGetRegexOrDefault(configuration.NoBumpMessage, RegexPatterns.VersionCalculation.DefaultNoBumpRegex);
        var versionBumpResetRegex = TryGetRegexOrDefault(
            configuration.VersionBumpResetMessage, RegexPatterns.VersionCalculation.DefaultVersionBumpResetRegex);

        CommitMessageIncrement? result = null;
        foreach (var commit in commits)
        {
            var commitMessageIncrement = GetIncrementFromCommit(
                commit, majorRegex, minorRegex, patchRegex, noBumpRegex, versionBumpResetRegex);
            if (!commitMessageIncrement.HasValue)
            {
                continue;
            }

            result = result.HasValue
                ? result.Value.Consolidate(commitMessageIncrement.Value)
                : commitMessageIncrement;

            if (commitMessageIncrement.Value.VersionBumpNeedsToBeReset)
            {
                break;
            }
        }

        return result;
    }

    private CommitMessageIncrement? FindCommitMessageIncrement(
        EffectiveConfiguration configuration, ICommit? baseVersionSource, ICommit currentCommit, string? label,
        IReadOnlySet<string>? includedCommits = null)
    {
        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
        {
            return null;
        }

        IEnumerable<ICommit> commits = GetCommitHistory(
            tagPrefix: configuration.TagPrefixPattern,
            semanticVersionFormat: configuration.SemanticVersionFormat,
            baseVersionSource: baseVersionSource,
            currentCommit: currentCommit,
            label: label,
            ignore: configuration.Ignore
        );

        if (includedCommits is not null)
        {
            commits = commits.Where(commit => includedCommits.Contains(commit.Sha));
        }

        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
        {
            commits = commits.Where(c => c.Parents.Count > 1);
        }

        return GetIncrementForCommits(configuration,
            commits: [.. commits]
        );
    }

    private CommitMessageIncrement? FindCommitMessageIncrement(
        EffectiveConfiguration configuration, IEnumerable<ICommit> commits, IReadOnlySet<string> commitHistory)
    {
        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
        {
            return null;
        }

        commits = commits.Where(commit => commitHistory.Contains(commit.Sha));
        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
        {
            commits = commits.Where(commit => commit.Parents.Count > 1);
        }

        return GetIncrementForCommits(configuration, [.. commits]);
    }

    private static Regex TryGetRegexOrDefault(string? messageRegex, Regex defaultRegex) =>
        messageRegex == null
            ? defaultRegex
            : RegexPatterns.Cache.GetOrAdd(messageRegex);

    private Dictionary<string, ICommit>.ValueCollection GetCommitHistory(string? tagPrefix, SemanticVersionFormat semanticVersionFormat,
        ICommit? baseVersionSource, ICommit currentCommit, string? label, IIgnoreConfiguration ignore)
    {
        var targetShas = new Lazy<HashSet<string>>(() =>
            [.. this.taggedSemanticVersionRepository
                .GetTaggedSemanticVersions(tagPrefix, semanticVersionFormat, ignore)
                .SelectMany(versionWithTags => versionWithTags)
                .Where(versionWithTag => versionWithTag.Tag.Commit.When <= Context.CurrentCommit.When)
                .Where(versionWithTag => versionWithTag.Value.IsMatchForBranchSpecificLabel(label))
                .Select(versionWithTag => versionWithTag.Tag.TargetSha)]
        );

        var intermediateCommits = this.repositoryStore.GetCommitLog(baseVersionSource, currentCommit, ignore);
        var commitLog = intermediateCommits.ToDictionary(element => element.Id.Sha);

        foreach (var intermediateCommit in intermediateCommits.Reverse())
        {
            if (!targetShas.Value.Contains(intermediateCommit.Sha) || !commitLog.Remove(intermediateCommit.Sha))
            {
                continue;
            }

            var parentCommits = intermediateCommit.Parents.ToList();
            while (parentCommits.Count != 0)
            {
                List<ICommit> temporaryList = [];
                foreach (var parentCommit in parentCommits.Where(parentCommit => commitLog.Remove(parentCommit.Sha)))
                {
                    temporaryList.AddRange(parentCommit.Parents);
                }
                parentCommits = temporaryList;
            }
        }

        return commitLog.Values;
    }

    /// <summary>
    /// Get the sequence of commits in a repository between a <paramref name="baseCommit"/> (exclusive)
    /// and a particular <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private ArraySegment<ICommit> GetIntermediateCommits(ICommit? baseCommit, ICommit headCommit, IIgnoreConfiguration ignore)
    {
        var map = GetHeadCommitsMap(headCommit, ignore);

        var commitAfterBaseIndex = 0;
        if (baseCommit != null)
        {
            if (!map.TryGetValue(baseCommit.Sha, out var baseIndex))
            {
                return [];
            }

            commitAfterBaseIndex = baseIndex + 1;
        }

        var headCommits = GetHeadCommits(headCommit, ignore);
        return new ArraySegment<ICommit>(headCommits, commitAfterBaseIndex, headCommits.Length - commitAfterBaseIndex);
    }

    /// <summary>
    /// Get a mapping of commit shas to their zero-based position in the sequence of commits from the beginning of a
    /// repository to a particular <paramref name="headCommit"/>
    /// </summary>
    private Dictionary<string, int> GetHeadCommitsMap(ICommit? headCommit, IIgnoreConfiguration ignore) =>
        this.headCommitsMapCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            GetHeadCommits(headCommit, ignore)
                .Select((commit, index) => (commit.Sha, Index: index))
                .ToDictionary(t => t.Sha, t => t.Index));

    /// <summary>
    /// Get the sequence of commits from the beginning of a repository to a particular
    /// <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private ICommit[] GetHeadCommits(ICommit? headCommit, IIgnoreConfiguration ignore) =>
        this.headCommitsCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            [.. this.repositoryStore.GetCommitsReacheableFromHead(headCommit, ignore)]);

    private CommitMessageIncrement? GetIncrementFromCommit(
        ICommit commit, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex noBumpRegex, Regex versionBumpResetRegex)
    {
        var key = new CommitIncrementCacheKey(
            commit.Sha, majorRegex, minorRegex, patchRegex, noBumpRegex, versionBumpResetRegex);
        return this.commitIncrementCache.GetOrAdd(key, () =>
        {
            var increment = GetIncrementFromMessage(commit.Message, majorRegex, minorRegex, patchRegex, noBumpRegex);
            if (!increment.HasValue)
            {
                return null;
            }

            return new(increment.Value, versionBumpResetRegex.IsMatch(commit.Message));
        });
    }

    private static VersionField? GetIncrementFromMessage(string message, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex noBumpRegex)
    {
        if (noBumpRegex.IsMatch(message))
        {
            return VersionField.None;
        }

        if (majorRegex.IsMatch(message))
        {
            return VersionField.Major;
        }

        if (minorRegex.IsMatch(message))
        {
            return VersionField.Minor;
        }

        if (patchRegex.IsMatch(message))
        {
            return VersionField.Patch;
        }

        return null;
    }

    public IEnumerable<ICommit> GetMergedCommits(ICommit mergeCommit, int index, IIgnoreConfiguration ignore)
    {
        mergeCommit.NotNull();

        if (!mergeCommit.IsMergeCommit)
        {
            throw new ArgumentException("The parameter is not a merge commit.", nameof(mergeCommit));
        }

        var baseCommit = mergeCommit.Parents[0];
        var mergedCommit = GetMergedHead(mergeCommit);
        if (index == 0)
        {
            (mergedCommit, baseCommit) = (baseCommit, mergedCommit);
        }

        var findMergeBase = this.repositoryStore.FindMergeBase(baseCommit, mergedCommit)
            ?? throw new InvalidOperationException("Cannot find the base commit of merged branch.");
        return GetIntermediateCommits(findMergeBase, mergedCommit, ignore);
    }

    private static ICommit GetMergedHead(ICommit mergeCommit)
    {
        var parents = mergeCommit.Parents.Skip(1).ToList();
        if (parents.Count > 1)
        {
            throw new NotSupportedException("GitVersion does not support more than one merge source in a single commit yet");
        }

        return parents.Single();
    }

    public CommitMessageIncrement GetIncrementForcedByCommit(ICommit commit, IGitVersionConfiguration configuration)
    {
        commit.NotNull();
        configuration.NotNull();

        var majorRegex = TryGetRegexOrDefault(configuration.MajorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMajorRegex);
        var minorRegex = TryGetRegexOrDefault(configuration.MinorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMinorRegex);
        var patchRegex = TryGetRegexOrDefault(configuration.PatchVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultPatchRegex);
        var none = TryGetRegexOrDefault(configuration.NoBumpMessage, RegexPatterns.VersionCalculation.DefaultNoBumpRegex);
        var versionBumpResetRegex = TryGetRegexOrDefault(
            configuration.VersionBumpResetMessage, RegexPatterns.VersionCalculation.DefaultVersionBumpResetRegex);

        return GetIncrementFromCommit(commit, majorRegex, minorRegex, patchRegex, none, versionBumpResetRegex) ?? default;
    }
}
