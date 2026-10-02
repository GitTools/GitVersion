using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal class IncrementStrategyFinder(
    IRepositoryStore repositoryStore,
    ITaggedSemanticVersionRepository taggedSemanticVersionRepository)
    : IIncrementStrategyFinder
{
    private static readonly IReadOnlySet<string> NothingExcluded = new HashSet<string>(StringComparer.Ordinal);

    private readonly Dictionary<string, CommitMessageIncrement?> commitIncrementCache = [];
    private readonly Dictionary<string, Dictionary<string, int>> headCommitsMapCache = [];
    private readonly Dictionary<string, ICommit[]> headCommitsCache = [];
    private readonly Dictionary<TaggedCommits, IReadOnlySet<string>> targetShasCache = [];
    private readonly Dictionary<VersionBumpResetScan, bool> versionBumpResetCache = [];

    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();

    // The store which GitVersion registers derives commit logs from a single cached revision walk. A store
    // supplied by an embedding application does not have to, in which case the original walk per base version
    // source is used, so replacing IRepositoryStore keeps working and simply forgoes the optimization.
    private readonly ICachedCommitLogProvider? commitLogProvider = repositoryStore as ICachedCommitLogProvider;

    public VersionField DetermineIncrementedField(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement, EffectiveConfiguration configuration, string? label)
    {
        currentCommit.NotNull();
        configuration.NotNull();

        var commitMessageIncrement = FindCommitMessageIncrement(configuration, baseVersionSource, currentCommit, label);

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
        EffectiveConfiguration configuration, ICommit? baseVersionSource, ICommit currentCommit, string? label)
    {
        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
        {
            return null;
        }

        IEnumerable<ICommit> commits = GetCommitHistory(
            configuration: configuration,
            baseVersionSource: baseVersionSource,
            currentCommit: currentCommit,
            label: label
        );

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

    /// <summary>
    /// Returns the commits between <paramref name="baseVersionSource"/> and <paramref name="currentCommit"/>
    /// whose messages may contribute to the increment, with the commits already covered by another version tag
    /// and everything they build upon left out.
    /// </summary>
    private IReadOnlyList<ICommit> GetCommitHistory(
        EffectiveConfiguration configuration, ICommit? baseVersionSource, ICommit currentCommit, string? label)
    {
        var tagPrefix = configuration.TagPrefixPattern;
        var semanticVersionFormat = configuration.SemanticVersionFormat;
        var ignore = configuration.Ignore;

        // Commits which are already covered by another version tag, and everything they build upon, must not
        // contribute to the increment. The set of those tags does not depend on the base version source, so it
        // is resolved once per tag configuration instead of once per commit log. A null label is not the same
        // as an empty one -- it matches every pre-release label -- so it has to stay distinct in the key.
        var targetShas = this.targetShasCache.GetOrAdd(new(tagPrefix ?? string.Empty, semanticVersionFormat, label, ignore), () =>
            (IReadOnlySet<string>)this.taggedSemanticVersionRepository
                .GetTaggedSemanticVersions(tagPrefix, semanticVersionFormat, ignore)
                .SelectMany(versionWithTags => versionWithTags)
                .Where(versionWithTag => versionWithTag.Value.IsMatchForBranchSpecificLabel(label))
                .Select(versionWithTag => versionWithTag.Tag.TargetSha)
                .ToHashSet(StringComparer.Ordinal));

        if (this.commitLogProvider is not { } provider)
        {
            return GetCommitHistoryFromIndividualWalk(baseVersionSource, currentCommit, ignore, targetShas);
        }

        if (ContainsVersionBumpReset(provider, configuration, currentCommit))
        {
            return GetCommitHistoryFromIndividualWalk(baseVersionSource, currentCommit, ignore, targetShas);
        }

        return provider.GetCommitLog(baseVersionSource, currentCommit, ignore, targetShas);
    }

    /// <summary>
    /// Reports whether any commit reachable from <paramref name="currentCommit"/> resets the accumulated version
    /// bump.
    /// </summary>
    /// <remarks>
    /// The optimized commit log is a subsequence of one revision walk. Its contents are identical to a walk which
    /// hides the base version source, but commits sharing a committer timestamp may be emitted in a different
    /// order, because the ordering of equally timed commits depends on the shape of the priority queue the walk
    /// maintains. <see cref="GetIncrementForCommits"/> consolidates the increments of all commits, which does not
    /// depend on their order, unless a commit resets the accumulated bump: it stops at the first such commit. Only
    /// then does the order become part of the result, so only then is the slower walk per base version source used.
    /// <para>
    /// The scan reads the full history through the same cached revision walk the optimized commit log is derived
    /// from, so it costs no additional walk. It deliberately asks about the whole history rather than about the
    /// commits of one base version source, which is conservative: a single commit resetting the bump anywhere in
    /// the history sends every base version source down the walking path, even the ones whose commit log does not
    /// contain it. That keeps the check to one pass over the history instead of one per base version source.
    /// </para>
    /// </remarks>
    private bool ContainsVersionBumpReset(
        ICachedCommitLogProvider provider, EffectiveConfiguration configuration, ICommit currentCommit)
    {
        var pattern = configuration.VersionBumpResetMessage
            ?? RegexPatterns.VersionCalculation.DefaultVersionBumpResetRegexPattern;

        return this.versionBumpResetCache.GetOrAdd(new(currentCommit.Sha, configuration.Ignore, pattern), () =>
        {
            var regex = TryGetRegexOrDefault(
                configuration.VersionBumpResetMessage, RegexPatterns.VersionCalculation.DefaultVersionBumpResetRegex);

            return provider.GetCommitLog(null, currentCommit, configuration.Ignore, NothingExcluded)
                .Any(commit => regex.IsMatch(commit.Message));
        });
    }

    /// <summary>
    /// The original commit history lookup, which asks for one revision walk per base version source and prunes the
    /// commits already covered by a version tag from the result.
    /// </summary>
    private IReadOnlyList<ICommit> GetCommitHistoryFromIndividualWalk(
        ICommit? baseVersionSource, ICommit currentCommit, IIgnoreConfiguration ignore, IReadOnlySet<string> targetShas)
    {
        var intermediateCommits = this.repositoryStore.GetCommitLog(baseVersionSource, currentCommit, ignore);
        var commitLog = intermediateCommits.ToDictionary(element => element.Id.Sha);

        foreach (var intermediateCommit in intermediateCommits.Reverse())
        {
            if (!targetShas.Contains(intermediateCommit.Sha) || !commitLog.Remove(intermediateCommit.Sha))
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

        return [.. commitLog.Values];
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

    /// <summary>Identifies the commits carrying a version tag which matches one tag configuration.</summary>
    private readonly record struct TaggedCommits(
        string TagPrefix, SemanticVersionFormat Format, string? Label, IIgnoreConfiguration Ignore);

    /// <summary>Identifies the search for a commit resetting the version bump in the history of one head commit.</summary>
    private readonly record struct VersionBumpResetScan(string HeadSha, IIgnoreConfiguration Ignore, string Pattern);
}
