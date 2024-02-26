using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

internal sealed class IncrementStrategyFinder : IIncrementStrategyFinder
{
    public const string DefaultMajorPattern = @"\+semver:\s?(breaking|major)";
    public const string DefaultMinorPattern = @"\+semver:\s?(feature|minor)";
    public const string DefaultPatchPattern = @"\+semver:\s?(fix|patch)";
    public const string DefaultNoBumpPattern = @"\+semver:\s?(none|skip)";

    private static readonly ConcurrentDictionary<string, Regex> CompiledRegexCache = new();
    private readonly Dictionary<string, VersionField?> commitIncrementCache = [];
    private readonly Dictionary<string, Dictionary<string, int>> headCommitsMapCache = [];
    private readonly Dictionary<string, ICommit[]> headCommitsCache = [];

    private static readonly Regex DefaultMajorPatternRegex = new(DefaultMajorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DefaultMinorPatternRegex = new(DefaultMinorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DefaultPatchPatternRegex = new(DefaultPatchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DefaultNoBumpPatternRegex = new(DefaultNoBumpPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IGitRepository repository;
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository;

    public IncrementStrategyFinder(IGitRepository repository, ITaggedSemanticVersionRepository taggedSemanticVersionRepository)
    {
        this.repository = repository.NotNull();
        this.taggedSemanticVersionRepository = taggedSemanticVersionRepository;
    }

    public VersionField DetermineIncrementedField(
        ICommit currentCommit, BaseVersion baseVersion, EffectiveConfiguration configuration, string? label)
    {
        baseVersion.NotNull();
        configuration.NotNull();

        var commitMessageIncrement = FindCommitMessageIncrement(
            configuration, baseVersion.BaseVersionSource, currentCommit, label);

        var defaultIncrement = configuration.Increment.ToVersionField();

        // use the default branch configuration increment strategy if there are no commit message overrides
        if (commitMessageIncrement == null)
        {
            return baseVersion.ShouldIncrement ? defaultIncrement : VersionField.None;
        }

        // don't increment for less than the branch configuration increment, if the absence of commit messages would have
        // still resulted in an increment of configuration.Increment
        if (baseVersion.ShouldIncrement && commitMessageIncrement < defaultIncrement)
        {
            return defaultIncrement;
        }

        return commitMessageIncrement.Value;
    }

    public VersionField? GetIncrementForCommits(string? majorVersionBumpMessage, string? minorVersionBumpMessage,
                                                string? patchVersionBumpMessage, string? noBumpMessage, ICommit[] commits)
    {
        commits.NotNull();

        var majorRegex = TryGetRegexOrDefault(majorVersionBumpMessage, DefaultMajorPatternRegex);
        var minorRegex = TryGetRegexOrDefault(minorVersionBumpMessage, DefaultMinorPatternRegex);
        var patchRegex = TryGetRegexOrDefault(patchVersionBumpMessage, DefaultPatchPatternRegex);
        var none = TryGetRegexOrDefault(noBumpMessage, DefaultNoBumpPatternRegex);

        var increments = commits
            .Select(c => GetIncrementFromCommit(c, majorRegex, minorRegex, patchRegex, none))
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
            label: label
        );

        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
        {
            commits = commits.Where(c => c.Parents.Count() > 1);
        }

        return GetIncrementForCommits(
            majorVersionBumpMessage: configuration.MajorVersionBumpMessage,
            minorVersionBumpMessage: configuration.MinorVersionBumpMessage,
            patchVersionBumpMessage: configuration.PatchVersionBumpMessage,
            noBumpMessage: configuration.NoBumpMessage,
            commits: commits.ToArray()
        );
    }

    private static Regex TryGetRegexOrDefault(string? messageRegex, Regex defaultRegex) =>
        messageRegex == null
            ? defaultRegex
            : CompiledRegexCache.GetOrAdd(messageRegex, pattern => new(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));

    public IReadOnlyCollection<ICommit> GetCommitHistory(
        string? tagPrefix, SemanticVersionFormat semanticVersionFormat, ICommit? baseVersionSource, ICommit currentCommit, string? label)
    {
        var targetShas = new Lazy<IReadOnlySet<string>>(() =>
            this.taggedSemanticVersionRepository.GetTaggedSemanticVersions(tagPrefix, semanticVersionFormat)
                .SelectMany(_ => _).Where(_ => _.Value.IsMatchForBranchSpecificLabel(label)).Select(_ => _.Tag.TargetSha).ToHashSet()
        );

        var intermediateCommits = GetIntermediateCommits(baseVersionSource, currentCommit).ToArray();

        var commitLog = intermediateCommits.ToDictionary(element => element.Id.Sha);

        foreach (var item in intermediateCommits.Reverse())
        {
            if (!commitLog.ContainsKey(item.Sha)) continue;

            if (targetShas.Value.Contains(item.Sha))
            {
                void RemoveCommitFromHistory(ICommit commit)
                {
                    if (!commitLog.ContainsKey(commit.Sha)) return;

                    commitLog.Remove(commit.Sha);
                    foreach (var item in commit.Parents)
                    {
                        RemoveCommitFromHistory(item);
                    }
                }
                RemoveCommitFromHistory(item);
            }
        }

        return commitLog.Values;
    }

    /// <summary>
    /// Get the sequence of commits in a repository between a <paramref name="baseCommit"/> (exclusive)
    /// and a particular <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private IEnumerable<ICommit> GetIntermediateCommits(ICommit? baseCommit, ICommit? headCommit)
    {
        var map = GetHeadCommitsMap(headCommit);

        var commitAfterBaseIndex = 0;
        if (baseCommit != null)
        {
            if (!map.TryGetValue(baseCommit.Sha, out var baseIndex)) return [];
            commitAfterBaseIndex = baseIndex + 1;
        }

        var headCommits = GetHeadCommits(headCommit);
        return new ArraySegment<ICommit>(headCommits, commitAfterBaseIndex, headCommits.Length - commitAfterBaseIndex);
    }

    /// <summary>
    /// Get a mapping of commit shas to their zero-based position in the sequence of commits from the beginning of a
    /// repository to a particular <paramref name="headCommit"/>
    /// </summary>
    private Dictionary<string, int> GetHeadCommitsMap(ICommit? headCommit) =>
        this.headCommitsMapCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            GetHeadCommits(headCommit)
                .Select((commit, index) => (commit.Sha, Index: index))
                .ToDictionary(t => t.Sha, t => t.Index));

    /// <summary>
    /// Get the sequence of commits from the beginning of a repository to a particular
    /// <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private ICommit[] GetHeadCommits(ICommit? headCommit) =>
        this.headCommitsCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            GetCommitsReacheableFromHead(headCommit).ToArray());

    private VersionField? GetIncrementFromCommit(ICommit commit, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex none) =>
        this.commitIncrementCache.GetOrAdd(commit.Sha, () =>
            GetIncrementFromMessage(commit.Message, majorRegex, minorRegex, patchRegex, none));

    private static VersionField? GetIncrementFromMessage(string message, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex none)
    {
        if (none.IsMatch(message)) return VersionField.None;
        if (majorRegex.IsMatch(message)) return VersionField.Major;
        if (minorRegex.IsMatch(message)) return VersionField.Minor;
        if (patchRegex.IsMatch(message)) return VersionField.Patch;
        return null;
    }

    private IEnumerable<ICommit> GetCommitsReacheableFromHead(ICommit? headCommit)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = headCommit,
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
        };

        return repository.Commits.QueryBy(filter);
    }

    public IEnumerable<ICommit> GetMergedCommits(ICommit mergeCommit, int index)
    {
        mergeCommit.NotNull();

        if (!mergeCommit.IsMergeCommit)
        {
            throw new ArgumentException("The parameter is not a merge commit.", nameof(mergeCommit));
        }

        ICommit baseCommit = mergeCommit.Parents.First();
        ICommit mergedCommit = GetMergedHead(mergeCommit);
        if (index == 0) (mergedCommit, baseCommit) = (baseCommit, mergedCommit);

        ICommit findMergeBase = this.repository.FindMergeBase(baseCommit, mergedCommit)
            ?? throw new InvalidOperationException("Cannot find the base commit of merged branch.");

        return GetIntermediateCommits(findMergeBase, mergedCommit);
    }

    private static ICommit GetMergedHead(ICommit mergeCommit)
    {
        var parents = mergeCommit.Parents.Skip(1).ToList();
        if (parents.Count > 1)
            throw new NotSupportedException("GitVersion does not support more than one merge source in a single commit yet");
        return parents.Single();
    }

    public VersionField GetIncrementForcedByCommit(ICommit commit, EffectiveConfiguration configuration)
    {
        commit.NotNull();
        configuration.NotNull();

        var majorRegex = TryGetRegexOrDefault(configuration.MajorVersionBumpMessage, DefaultMajorPatternRegex);
        var minorRegex = TryGetRegexOrDefault(configuration.MinorVersionBumpMessage, DefaultMinorPatternRegex);
        var patchRegex = TryGetRegexOrDefault(configuration.PatchVersionBumpMessage, DefaultPatchPatternRegex);
        var none = TryGetRegexOrDefault(configuration.NoBumpMessage, DefaultNoBumpPatternRegex);

        return GetIncrementFromCommit(commit, majorRegex, minorRegex, patchRegex, none) ?? VersionField.None;
    }
}
