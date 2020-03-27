using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion
{
    public enum CommitMessageIncrementMode
    {
        Enabled,
        Disabled,
        MergeMessageOnly
    }

    public static class IncrementStrategyFinder
    {
        private static List<Commit> intermediateCommitCache;
        public const string DefaultMajorPattern = @"\+semver:\s?(breaking|major)";
        public const string DefaultMinorPattern = @"\+semver:\s?(feature|minor)";
        public const string DefaultPatchPattern = @"\+semver:\s?(fix|patch)";
        public const string DefaultNoBumpPattern = @"\+semver:\s?(none|skip)";

        private static readonly ConcurrentDictionary<string, Regex> CompiledRegexCache = new ConcurrentDictionary<string, Regex>();

        private static readonly Regex DefaultMajorPatternRegex = new Regex(DefaultMajorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DefaultMinorPatternRegex = new Regex(DefaultMinorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DefaultPatchPatternRegex = new Regex(DefaultPatchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DefaultNoBumpPatternRegex = new Regex(DefaultNoBumpPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static VersionField? DetermineIncrementedField(IRepository repository, GitVersionContext context, BaseVersion baseVersion)
        {
            var commitMessageIncrement = FindCommitMessageIncrement(repository, context, baseVersion);
            var defaultIncrement = context.Configuration.Increment.ToVersionField();

            // use the default branch config increment strategy if there are no commit message overrides
            if (commitMessageIncrement == null)
            {
                return baseVersion.ShouldIncrement ? defaultIncrement : (VersionField?)null;
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
        public static VersionField FindDefaultIncrementForBranch(GitVersionContext context, string branch = null)
        {
            var config = context.FullConfiguration.GetConfigForBranch(branch ?? context.CurrentBranch.NameWithoutRemote());
            if (config?.Increment != null && config.Increment != IncrementStrategy.Inherit)
            {
                return config.Increment.Value.ToVersionField();
            }

            // Fallback to patch
            return VersionField.Patch;
        }
        public static VersionField? GetIncrementForCommits(GitVersionContext context, IEnumerable<Commit> commits)
        {
            var majorRegex = TryGetRegexOrDefault(context.Configuration.MajorVersionBumpMessage, DefaultMajorPatternRegex);
            var minorRegex = TryGetRegexOrDefault(context.Configuration.MinorVersionBumpMessage, DefaultMinorPatternRegex);
            var patchRegex = TryGetRegexOrDefault(context.Configuration.PatchVersionBumpMessage, DefaultPatchPatternRegex);
            var none = TryGetRegexOrDefault(context.Configuration.NoBumpMessage, DefaultNoBumpPatternRegex);

            var increments = commits
                .Select(c => GetIncrementFromMessage(c.Message, majorRegex, minorRegex, patchRegex, none))
                .Where(v => v != null)
                .Select(v => v.Value)
                .ToList();

            if (increments.Any())
            {
                return increments.Max();
            }

            return null;
        }

        private static VersionField? FindCommitMessageIncrement(IRepository repository, GitVersionContext context, BaseVersion baseVersion)
        {
            if (context.Configuration.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
            {
                return null;
            }

            var commits = GetIntermediateCommits(repository, baseVersion.BaseVersionSource, context.CurrentCommit);

            if (context.Configuration.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
            {
                commits = commits.Where(c => c.Parents.Count() > 1);
            }

            return GetIncrementForCommits(context, commits);
        }
        private static Regex TryGetRegexOrDefault(string messageRegex, Regex defaultRegex)
        {
            if (messageRegex == null)
            {
                return defaultRegex;
            }

            return CompiledRegexCache.GetOrAdd(messageRegex, pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        }
        private static IEnumerable<Commit> GetIntermediateCommits(IRepository repo, GitObject baseCommit, Commit headCommit)
        {
            if (baseCommit == null) yield break;

            var commitCache = intermediateCommitCache;

            if (commitCache == null || commitCache.LastOrDefault() != headCommit)
            {
                commitCache = GetCommitsReacheableFromHead(repo, headCommit);
                intermediateCommitCache = commitCache;
            }

            var found = false;
            foreach (var commit in commitCache)
            {
                if (found)
                    yield return commit;

                if (commit.Sha == baseCommit.Sha)
                    found = true;
            }
        }
        private static VersionField? GetIncrementFromMessage(string message, Regex majorRegex, Regex minorRegex, Regex patchRegex, Regex none)
        {
            if (majorRegex.IsMatch(message)) return VersionField.Major;
            if (minorRegex.IsMatch(message)) return VersionField.Minor;
            if (patchRegex.IsMatch(message)) return VersionField.Patch;
            if (none.IsMatch(message)) return VersionField.None;
            return null;
        }

        private static List<Commit> GetCommitsReacheableFromHead(IRepository repository, Commit headCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = headCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            return repository.Commits.QueryBy(filter).ToList();
        }
    }
}
