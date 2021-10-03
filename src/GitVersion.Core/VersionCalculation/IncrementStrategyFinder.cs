using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GitVersion.Extensions;

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

    public VersionField? DetermineIncrementedField(IGitRepository repository, GitVersionContext context, BaseVersion baseVersion)
    {
        var commitMessageIncrement = FindCommitMessageIncrement(repository, context, baseVersion.BaseVersionSource);

        var defaultIncrement = context.Configuration?.Increment.ToVersionField();

        // use the default branch config increment strategy if there are no commit message overrides
        if (commitMessageIncrement == null)
        {
            return baseVersion.ShouldIncrement ? defaultIncrement : null;
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

        return commitMessageIncrement;
    }

    public VersionField? GetIncrementForCommits(GitVersionContext context, IEnumerable<ICommit> commits)
    {
        var majorRegex = TryGetRegexOrDefault(context.Configuration?.MajorVersionBumpMessage, DefaultMajorPatternRegex);
        var minorRegex = TryGetRegexOrDefault(context.Configuration?.MinorVersionBumpMessage, DefaultMinorPatternRegex);
        var patchRegex = TryGetRegexOrDefault(context.Configuration?.PatchVersionBumpMessage, DefaultPatchPatternRegex);
        var none = TryGetRegexOrDefault(context.Configuration?.NoBumpMessage, DefaultNoBumpPatternRegex);

        var increments = commits
            .Select(c => GetIncrementFromCommit(c, majorRegex, minorRegex, patchRegex, none))
            .Where(v => v != null)
            .Select(v => v!.Value)
            .ToList();

        if (increments.Any())
        {
            return increments.Max();
        }

        return null;
    }

    private VersionField? FindCommitMessageIncrement(IGitRepository repository, GitVersionContext context, ICommit? baseCommit)
    {
        if (baseCommit == null) return null;

        if (context.Configuration?.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
        {
            return null;
        }

        var commits = GetIntermediateCommits(repository, baseCommit, context.CurrentCommit);

        if (context.Configuration?.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
        {
            commits = commits.Where(c => c.Parents.Count() > 1);
        }

        return GetIncrementForCommits(context, commits);
    }

    private static Regex TryGetRegexOrDefault(string? messageRegex, Regex defaultRegex)
    {
        if (messageRegex == null)
        {
            return defaultRegex;
        }

        return CompiledRegexCache.GetOrAdd(messageRegex, pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Get the sequence of commits in a <paramref name="repo"/> between a <paramref name="baseCommit"/> (exclusive)
    /// and a particular <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private IEnumerable<ICommit> GetIntermediateCommits(IGitRepository repo, ICommit baseCommit, ICommit? headCommit)
    {
        var map = GetHeadCommitsMap(repo, headCommit);
        if (!map.TryGetValue(baseCommit.Sha, out var baseIndex)) return Enumerable.Empty<ICommit>();
        var commitAfterBaseIndex = baseIndex + 1;
        var headCommits = GetHeadCommits(repo, headCommit);
        return new ArraySegment<ICommit>(headCommits, commitAfterBaseIndex, headCommits.Length - commitAfterBaseIndex);
    }

    /// <summary>
    /// Get a mapping of commit shas to their zero-based position in the sequence of commits from the beginning of a
    /// <paramref name="repo"/> to a particular <paramref name="headCommit"/>
    /// </summary>
    private Dictionary<string, int> GetHeadCommitsMap(IGitRepository repo, ICommit? headCommit) =>
        this.headCommitsMapCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            GetHeadCommits(repo, headCommit)
                .Select((commit, index) => (Sha: commit.Sha, Index: index))
                .ToDictionary(t => t.Sha, t => t.Index));

    /// <summary>
    /// Get the sequence of commits from the beginning of a <paramref name="repo"/> to a particular
    /// <paramref name="headCommit"/> (inclusive)
    /// </summary>
    private ICommit[] GetHeadCommits(IGitRepository repo, ICommit? headCommit) =>
        this.headCommitsCache.GetOrAdd(headCommit?.Sha ?? "NULL", () =>
            GetCommitsReacheableFromHead(repo, headCommit).ToArray());

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
