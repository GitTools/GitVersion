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
    private readonly Dictionary<string, CommitMessageIncrement?> commitIncrementCache = [];
    private readonly Dictionary<(string Commit, EffectiveConfiguration Target), VersionField[]> mergedBranchIncrementCache = [];
    private readonly Dictionary<(string Branch, string? Tip), EffectiveBranchConfiguration[]> effectiveBranchConfigurationCache = [];
    private readonly Dictionary<string, HashSet<string>> firstParentHistoryCache = [];
    private readonly Dictionary<string, Dictionary<string, int>> headCommitsMapCache = [];
    private readonly Dictionary<string, ICommit[]> headCommitsCache = [];

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

        if (!configuration.IsMainBranch
            || !configuration.TrackMergeMessage
            || Context.Configuration.GetBranchConfiguration(Context.CurrentBranch.Name).IsMainBranch != true)
        {
            return targetIncrement;
        }

        var increments = GetIncrementsFromCommitHistory(
            currentCommit, baseVersionSource, shouldIncrement, configuration, label, targetIncrement).ToArray();

        return increments.Length == 0
            ? targetIncrement
            : increments.Aggregate(VersionField.None, (result, increment) => result.Consolidate(increment));
    }

    private VersionField DetermineIncrementedFieldInternal(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement,
        EffectiveConfiguration configuration, string? label,
        IReadOnlySet<string>? includedCommits = null)
    {
        var commitMessageIncrement = FindCommitMessageIncrement(
            configuration, baseVersionSource, currentCommit, label, includedCommits);

        var defaultIncrement = configuration.Increment.ToVersionField();

        // use the default branch configuration increment strategy if there are no commit message overrides
        if (commitMessageIncrement == null)
        {
            return shouldIncrement ? defaultIncrement : VersionField.None;
        }

        // don't increment for less than the branch configuration increment, if the absence of commit messages would have
        // still resulted in an increment of configuration.Increment
        if (shouldIncrement && !commitMessageIncrement.Value.VersionBumpNeedsToBeReset
            && commitMessageIncrement.Value.Increment < defaultIncrement)
        {
            return defaultIncrement;
        }

        return commitMessageIncrement.Value.Increment;
    }

    private IEnumerable<VersionField> GetIncrementsFromCommitHistory(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement,
        EffectiveConfiguration targetConfiguration, string? targetLabel, VersionField targetIncrement)
    {
        var configuration = Context.Configuration;
        var commitLog = this.repositoryStore
            .GetCommitLog(baseVersionSource, currentCommit, targetConfiguration.Ignore)
            .Select(commit => commit.Sha)
            .ToHashSet();
        List<ICommit> targetCommits = [];
        Dictionary<string, (ICommit Commit, ReferenceName MergedBranch)> mergedBranches = [];

        for (ICommit? commit = currentCommit; commit is not null; commit = commit.Parents.FirstOrDefault())
        {
            if (baseVersionSource?.Equals(commit) == true)
            {
                break;
            }
            if (!commitLog.Contains(commit.Sha))
            {
                continue;
            }

            targetCommits.Add(commit);
            if (!commit.IsMergeCommit
                || commit.Parents.Count != 2
                || !MergeMessage.TryParse(commit, configuration, out var mergeMessage)
                || mergeMessage.MergedBranch is not { } mergedBranch)
            {
                continue;
            }

            mergedBranches.Add(commit.Sha, (commit, mergedBranch));
        }

        if (mergedBranches.Count == 0)
        {
            yield return targetIncrement;
            yield break;
        }

        var targetCommitShas = targetCommits.Select(commit => commit.Sha).ToHashSet();
        targetIncrement = DetermineIncrementedFieldInternal(
            currentCommit, baseVersionSource, shouldIncrement,
            targetConfiguration, targetLabel, targetCommitShas);

        var mergedBranchCommits = mergedBranches.Keys.ToHashSet();
        var hasTargetContribution = targetCommits.Any(commit => !mergedBranchCommits.Contains(commit.Sha))
            || FindCommitMessageIncrement(
                targetConfiguration, baseVersionSource, currentCommit, targetLabel, mergedBranchCommits) is not null;
        if (hasTargetContribution)
        {
            yield return targetIncrement;
        }

        foreach (var (commit, mergedBranch) in mergedBranches.Values)
        {
            var sourceBranchConfiguration = configuration.GetBranchConfiguration(mergedBranch);
            var preventIncrementWhenBranchMerged = sourceBranchConfiguration.PreventIncrement.WhenBranchMerged
                ?? configuration.PreventIncrement.WhenBranchMerged;

            foreach (var sourceIncrement in this.mergedBranchIncrementCache.GetOrAdd(
                         (commit.Sha, targetConfiguration), () =>
                         {
                             var mergeBase = this.repositoryStore.FindMergeBase(commit.Parents[0], commit.Parents[1]);

                             return [.. GetSourceConfigurations(
                                     mergedBranch, commit.Parents[1], sourceBranchConfiguration, targetConfiguration)
                                 .Select(sourceConfiguration => DetermineIncrementedFieldInternal(
                                     currentCommit: commit.Parents[1],
                                     baseVersionSource: mergeBase,
                                     shouldIncrement: true,
                                     configuration: sourceConfiguration,
                                     label: sourceConfiguration.GetBranchSpecificLabel(
                                         mergedBranch, null, this.environment)
                                 ))];
                         }))
            {
                yield return SelectIncrement(
                    targetConfiguration.PreventIncrementOfMergedBranch,
                    preventIncrementWhenBranchMerged,
                    targetIncrement,
                    sourceIncrement
                );
            }
        }
    }

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
            var configurations = GetEffectiveBranchConfigurations(existingBranch)
                .Select(candidate => candidate.Value)
                .Distinct()
                .ToArray();
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
                Context.CurrentBranch.Name, this.repositoryStore)
            .SelectMany(GetEffectiveBranchConfigurations)
            .Select(source => new EffectiveConfiguration(
                Context.Configuration, sourceBranchConfiguration, source.Value))
            .Distinct()
            .ToArray();

        return inheritedConfigurations.Length != 0
            ? inheritedConfigurations
            : [Context.Configuration.GetEffectiveConfiguration(mergedBranch, targetConfiguration)];
    }

    private EffectiveBranchConfiguration[] GetEffectiveBranchConfigurations(IBranch branch) =>
        this.effectiveBranchConfigurationCache.GetOrAdd(
            (branch.Name.ToString(), branch.Tip?.Sha),
            () => [.. this.effectiveBranchConfigurationFinder
                .GetConfigurations(branch, Context.Configuration)
            ]);

    private IEnumerable<IBranch> FindClosestSourceBranches(
        ICommit mergedBranchTip, IBranchConfiguration mergedBranchConfiguration,
        IGitVersionConfiguration configuration, ReferenceName currentBranch,
        IRepositoryStore repositoryStore)
    {
        var candidates = repositoryStore.Branches
            .Where(branch => IsConfiguredSourceBranch(branch, mergedBranchConfiguration, configuration)
                // The current target contains the merged tip through this merge and cannot be the
                // topic's source. Other configured source branches may legitimately absorb it later.
                && !branch.Name.EquivalentTo(currentBranch.WithoutOrigin))
            .GroupBy(branch => branch.Name.WithoutOrigin, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MinBy(branch => branch.IsRemote))
            .OfType<IBranch>();

        var closestDistance = int.MaxValue;
        List<IBranch> result = [];
        foreach (var candidate in candidates)
        {
            if (candidate.Tip is null)
            {
                continue;
            }

            if (GetFirstParentSourceDistance(mergedBranchTip, candidate.Tip) is not { } distance)
            {
                continue;
            }
            if (distance < closestDistance)
            {
                closestDistance = distance;
                result.Clear();
            }
            if (distance == closestDistance)
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private int? GetFirstParentSourceDistance(ICommit mergedBranchTip, ICommit sourceBranchTip)
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
                return distance;
            }
            distance++;
        }
        return null;
    }

    private static bool IsConfiguredSourceBranch(
        IBranch candidate, IBranchConfiguration mergedBranchConfiguration,
        IGitVersionConfiguration configuration) =>
        mergedBranchConfiguration.SourceBranches.Any(sourceBranch =>
            configuration.Branches.TryGetValue(sourceBranch, out var sourceBranchConfiguration)
            && sourceBranchConfiguration.IsMatch(candidate.Name.WithoutOrigin));

    private static VersionField SelectIncrement(
        bool preventIncrementOfMergedBranch, bool? preventIncrementWhenBranchMerged,
        VersionField targetIncrement, VersionField sourceIncrement)
    {
        if (preventIncrementOfMergedBranch)
        {
            return preventIncrementWhenBranchMerged == true ? targetIncrement : sourceIncrement;
        }

        return preventIncrementWhenBranchMerged is null
            ? targetIncrement.Consolidate(sourceIncrement)
            : targetIncrement;
    }

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
        ICommit commit, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex noBumpRegex, Regex versionBumpResetRegex) =>
        this.commitIncrementCache.GetOrAdd(commit.Sha, () =>
        {
            var increment = GetIncrementFromMessage(commit.Message, majorRegex, minorRegex, patchRegex, noBumpRegex);
            if (!increment.HasValue)
            {
                return null;
            }

            return new(increment.Value, versionBumpResetRegex.IsMatch(commit.Message));
        });

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
