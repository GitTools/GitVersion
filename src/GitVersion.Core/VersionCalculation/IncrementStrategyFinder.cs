using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public class IncrementStrategyFinder : IIncrementStrategyFinder
{
    public const string DefaultMajorPattern = @"\+semver:\s?(breaking|major)";
    public const string DefaultMinorPattern = @"\+semver:\s?(feature|minor)";
    public const string DefaultPatchPattern = @"\+semver:\s?(fix|patch)";
    public const string DefaultNoBumpPattern = @"\+semver:\s?(none|skip)";

    private static readonly ConcurrentDictionary<string, Regex> CompiledRegexCache = new();
    private readonly Dictionary<string, VersionField?> commitIncrementCache = new();
    private readonly Dictionary<string, Dictionary<string, int>> headCommitsMapCache = new();
    private readonly Dictionary<string, ICommit[]> headCommitsCache = new();

    private static readonly Regex DefaultMajorPatternRegex = new(DefaultMajorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DefaultMinorPatternRegex = new(DefaultMinorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DefaultPatchPatternRegex = new(DefaultPatchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DefaultNoBumpPatternRegex = new(DefaultNoBumpPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IGitRepository repository;

    public IncrementStrategyFinder(IGitRepository repository) => this.repository = repository.NotNull();

    public VersionField DetermineIncrementedField(GitVersionContext context, BaseVersion baseVersion, EffectiveConfiguration configuration)
    {
        configuration.NotNull();

        var commitMessageIncrement = FindCommitMessageIncrement(this.repository, context, configuration, baseVersion.BaseVersionSource);

        var defaultIncrement = configuration.Increment.ToVersionField();

        // use the default branch config increment strategy if there are no commit message overrides
        if (commitMessageIncrement == null)
        {
            return baseVersion.ShouldIncrement ? defaultIncrement : VersionField.None;
        }

        // cap the commit message severity to minor for alpha versions
        if (baseVersion.SemanticVersion < new SemanticVersion(1) && commitMessageIncrement > VersionField.Minor)
        {
            commitMessageIncrement = VersionField.Minor;
        }

        // don't increment for less than the branch config increment, if the absence of commit messages would have
        // still resulted in an increment of configuration.Increment
        if (baseVersion.ShouldIncrement && commitMessageIncrement < defaultIncrement)
        {
            return defaultIncrement;
        }

        return commitMessageIncrement.Value;
    }

    public VersionField? GetIncrementForCommits(Config configuration, IEnumerable<ICommit> commits)
    {
        var majorRegex = TryGetRegexOrDefault(configuration.MajorVersionBumpMessage, DefaultMajorPatternRegex);
        var minorRegex = TryGetRegexOrDefault(configuration.MinorVersionBumpMessage, DefaultMinorPatternRegex);
        var patchRegex = TryGetRegexOrDefault(configuration.PatchVersionBumpMessage, DefaultPatchPatternRegex);
        var none = TryGetRegexOrDefault(configuration.NoBumpMessage, DefaultNoBumpPatternRegex);

        var increments = commits
            .Select(c => GetIncrementFromCommit(c, majorRegex, minorRegex, patchRegex, none))
            .Where(v => v != null)
            .ToList();

        return increments.Any()
            ? increments.Max()
            : null;
    }

    private VersionField? FindCommitMessageIncrement(IGitRepository repository, GitVersionContext context,
        EffectiveConfiguration configuration, ICommit? baseCommit)
    {
        if (baseCommit == null) return null;

        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
        {
            return null;
        }

        var commits = GetIntermediateCommits(repository, baseCommit, context.CurrentCommit);

        // consider commit messages since latest tag only (see #3071)
        var tags = new HashSet<string?>(repository.Tags.Select(t => t.TargetSha));
        commits = commits
            .Reverse()
            .TakeWhile(x => !tags.Contains(x.Sha))
            .Reverse();

        if (configuration.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
        {
            commits = commits.Where(c => c.Parents.Count() > 1);
        }

        return GetIncrementForCommits(context.FullConfiguration, commits);
    }

    private static Regex TryGetRegexOrDefault(string? messageRegex, Regex defaultRegex) =>
        messageRegex == null
            ? defaultRegex
            : CompiledRegexCache.GetOrAdd(messageRegex, pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));

    /// <summary>
    /// Get the sequence of commits in a <paramref name="repository"/> between a <paramref name="baseCommit"/> (exclusive)
    /// and a particular <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private IEnumerable<ICommit> GetIntermediateCommits(IGitRepository repository, IGitObject baseCommit, ICommit? headCommit)
    {
        var map = GetHeadCommitsMap(repository, headCommit);
        if (!map.TryGetValue(baseCommit.Sha, out var baseIndex)) return Enumerable.Empty<ICommit>();
        var commitAfterBaseIndex = baseIndex + 1;
        var headCommits = GetHeadCommits(repository, headCommit);
        return new ArraySegment<ICommit>(headCommits, commitAfterBaseIndex, headCommits.Length - commitAfterBaseIndex);
    }

    /// <summary>
    /// Get a mapping of commit shas to their zero-based position in the sequence of commits from the beginning of a
    /// <paramref name="repository"/> to a particular <paramref name="headCommit"/>
    /// </summary>
    private Dictionary<string, int> GetHeadCommitsMap(IGitRepository repository, ICommit? headCommit) =>
        this.headCommitsMapCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            GetHeadCommits(repository, headCommit)
                .Select((commit, index) => (commit.Sha, Index: index))
                .ToDictionary(t => t.Sha, t => t.Index));

    /// <summary>
    /// Get the sequence of commits from the beginning of a <paramref name="repository"/> to a particular
    /// <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private ICommit[] GetHeadCommits(IGitRepository repository, ICommit? headCommit) =>
        this.headCommitsCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            GetCommitsReacheableFromHead(repository, headCommit).ToArray());

    private VersionField? GetIncrementFromCommit(ICommit commit, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex none) =>
        this.commitIncrementCache.GetOrAdd(commit.Sha, () =>
            GetIncrementFromMessage(commit.Message, majorRegex, minorRegex, patchRegex, none));

    private static VersionField? GetIncrementFromMessage(string message, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex none)
    {
        if (majorRegex.IsMatch(message)) return VersionField.Major;
        if (minorRegex.IsMatch(message)) return VersionField.Minor;
        if (patchRegex.IsMatch(message)) return VersionField.Patch;
        if (none.IsMatch(message)) return VersionField.None;
        return null;
    }

    /// <summary>
    /// Query a <paramref name="repo"/> for the sequence of commits from the beginning to a particular
    /// <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private static IEnumerable<ICommit> GetCommitsReacheableFromHead(IGitRepository repo, ICommit? headCommit)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = headCommit,
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
        };

        return repo.Commits.QueryBy(filter);
    }
}
