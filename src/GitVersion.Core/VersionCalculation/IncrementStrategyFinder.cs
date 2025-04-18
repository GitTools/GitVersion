using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal class IncrementStrategyFinder(IRepositoryStore repositoryStore, ITaggedSemanticVersionRepository taggedSemanticVersionRepository)
    : IIncrementStrategyFinder
{
    private readonly Dictionary<string, VersionField?> commitIncrementCache = [];
    private readonly Dictionary<string, Dictionary<string, int>> headCommitsMapCache = [];
    private readonly Dictionary<string, ICommit[]> headCommitsCache = [];

    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();

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
        if (shouldIncrement && commitMessageIncrement < defaultIncrement)
        {
            return defaultIncrement;
        }

        return commitMessageIncrement.Value;
    }

    private VersionField? GetIncrementForCommits(EffectiveConfiguration configuration, ICommit[] commits)
    {
        commits.NotNull();

        var majorRegex = TryGetRegexOrDefault(configuration.MajorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMajorRegex);
        var minorRegex = TryGetRegexOrDefault(configuration.MinorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMinorRegex);
        var patchRegex = TryGetRegexOrDefault(configuration.PatchVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultPatchRegex);
        var noBumpRegex = TryGetRegexOrDefault(configuration.NoBumpMessage, RegexPatterns.VersionCalculation.DefaultNoBumpRegex);

        var increments = commits
            .Select(c => GetIncrementFromCommit(c, majorRegex, minorRegex, patchRegex, noBumpRegex))
            .Where(v => v != null)
            .ToList();

        return increments.Count != 0
            ? increments.Max()
            : null;
    }

    private VersionField? FindCommitMessageIncrement(
        EffectiveConfiguration configuration, ICommit? baseVersionSource, ICommit currentCommit, string? label)
    {
        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
        {
            return null;
        }

        IEnumerable<ICommit> commits = GetCommitHistory(
            tagPrefix: configuration.TagPrefix,
            semanticVersionFormat: configuration.SemanticVersionFormat,
            baseVersionSource: baseVersionSource,
            currentCommit: currentCommit,
            label: label,
            ignore: configuration.Ignore
        );

        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
        {
            commits = commits.Where(c => c.Parents.Count > 1);
        }

        return GetIncrementForCommits(configuration,
            commits: commits.ToArray()
        );
    }

    private static Regex TryGetRegexOrDefault(string? messageRegex, Regex defaultRegex) =>
        messageRegex == null
            ? defaultRegex
            : RegexPatterns.Cache.GetOrAdd(messageRegex);

    private IReadOnlyCollection<ICommit> GetCommitHistory(string? tagPrefix, SemanticVersionFormat semanticVersionFormat,
        ICommit? baseVersionSource, ICommit currentCommit, string? label, IIgnoreConfiguration ignore)
    {
        var targetShas = new Lazy<HashSet<string>>(() =>
            taggedSemanticVersionRepository
                .GetTaggedSemanticVersions(tagPrefix, semanticVersionFormat, ignore)
                .SelectMany(versionWithTags => versionWithTags)
                .Where(versionWithTag => versionWithTag.Value.IsMatchForBranchSpecificLabel(label))
                .Select(versionWithTag => versionWithTag.Tag.TargetSha)
                .ToHashSet()
        );

        var intermediateCommits = this.repositoryStore.GetCommitLog(baseVersionSource, currentCommit, ignore);
        var commitLog = intermediateCommits.ToDictionary(element => element.Id.Sha);

        foreach (var intermediateCommit in intermediateCommits.Reverse())
        {
            if (targetShas.Value.Contains(intermediateCommit.Sha) && commitLog.Remove(intermediateCommit.Sha))
            {
                var parentCommits = intermediateCommit.Parents.ToList();
                while (parentCommits.Count != 0)
                {
                    List<ICommit> temporaryList = [];
                    foreach (var parentCommit in parentCommits)
                    {
                        if (commitLog.Remove(parentCommit.Sha))
                        {
                            temporaryList.AddRange(parentCommit.Parents);
                        }
                    }
                    parentCommits = temporaryList;
                }
            }
        }

        return commitLog.Values;
    }

    /// <summary>
    /// Get the sequence of commits in a repository between a <paramref name="baseCommit"/> (exclusive)
    /// and a particular <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private IEnumerable<ICommit> GetIntermediateCommits(ICommit? baseCommit, ICommit headCommit, IIgnoreConfiguration ignore)
    {
        var map = GetHeadCommitsMap(headCommit, ignore);

        var commitAfterBaseIndex = 0;
        if (baseCommit != null)
        {
            if (!map.TryGetValue(baseCommit.Sha, out var baseIndex)) return [];
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

    private VersionField? GetIncrementFromCommit(ICommit commit, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex noBumpRegex) =>
        this.commitIncrementCache.GetOrAdd(commit.Sha, () =>
            GetIncrementFromMessage(commit.Message, majorRegex, minorRegex, patchRegex, noBumpRegex));

    private static VersionField? GetIncrementFromMessage(string message, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex noBumpRegex)
    {
        if (noBumpRegex.IsMatch(message)) return VersionField.None;
        if (majorRegex.IsMatch(message)) return VersionField.Major;
        if (minorRegex.IsMatch(message)) return VersionField.Minor;
        if (patchRegex.IsMatch(message)) return VersionField.Patch;
        return null;
    }

    public IEnumerable<ICommit> GetMergedCommits(ICommit mergeCommit, int index, IIgnoreConfiguration ignore)
    {
        mergeCommit.NotNull();

        if (!mergeCommit.IsMergeCommit())
        {
            throw new ArgumentException("The parameter is not a merge commit.", nameof(mergeCommit));
        }

        var baseCommit = mergeCommit.Parents[0];
        var mergedCommit = GetMergedHead(mergeCommit);
        if (index == 0) (mergedCommit, baseCommit) = (baseCommit, mergedCommit);

        var findMergeBase = this.repositoryStore.FindMergeBase(baseCommit, mergedCommit)
            ?? throw new InvalidOperationException("Cannot find the base commit of merged branch.");
        return GetIntermediateCommits(findMergeBase, mergedCommit, ignore);
    }

    private static ICommit GetMergedHead(ICommit mergeCommit)
    {
        var parents = mergeCommit.Parents.Skip(1).ToList();
        if (parents.Count > 1)
            throw new NotSupportedException("GitVersion does not support more than one merge source in a single commit yet");
        return parents.Single();
    }

    public VersionField GetIncrementForcedByCommit(ICommit commit, IGitVersionConfiguration configuration)
    {
        commit.NotNull();
        configuration.NotNull();

        var majorRegex = TryGetRegexOrDefault(configuration.MajorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMajorRegex);
        var minorRegex = TryGetRegexOrDefault(configuration.MinorVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultMinorRegex);
        var patchRegex = TryGetRegexOrDefault(configuration.PatchVersionBumpMessage, RegexPatterns.VersionCalculation.DefaultPatchRegex);
        var none = TryGetRegexOrDefault(configuration.NoBumpMessage, RegexPatterns.VersionCalculation.DefaultNoBumpRegex);

        return GetIncrementFromCommit(commit, majorRegex, minorRegex, patchRegex, none) ?? VersionField.None;
    }
}
