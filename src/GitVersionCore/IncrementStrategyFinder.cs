using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;
using GitVersion.Configuration;
using GitVersion.Extensions;

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

        public static VersionField? DetermineIncrementedField(GitVersionContext context, BaseVersion baseVersion)
        {
            var commitMessageIncrement = FindCommitMessageIncrement(context, baseVersion);
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

        private static VersionField? FindCommitMessageIncrement(GitVersionContext context, BaseVersion baseVersion)
        {
            if (context.Configuration.CommitMessageIncrementing == CommitMessageIncrementMode.Disabled)
            {
                return null;
            }

            var commits = GetIntermediateCommits(context.Repository, baseVersion.BaseVersionSource, context.CurrentCommit);

            if (context.Configuration.CommitMessageIncrementing == CommitMessageIncrementMode.MergeMessageOnly)
            {
                commits = commits.Where(c => c.Parents.Count() > 1);
            }

            return GetIncrementForCommits(context, commits);
        }

        public static VersionField? GetIncrementForCommits(GitVersionContext context, IEnumerable<Commit> commits)
        {
            // More efficient use of Regexes. The static version of Regex.IsMatch caches the compiled regexes.
            // see:  https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices#static-regular-expressions

            var majorRegex = context.Configuration.MajorVersionBumpMessage ?? DefaultMajorPattern;
            var minorRegex = context.Configuration.MinorVersionBumpMessage ?? DefaultMinorPattern;
            var patchRegex = context.Configuration.PatchVersionBumpMessage ?? DefaultPatchPattern;
            var none = context.Configuration.NoBumpMessage ?? DefaultNoBumpPattern;

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

        private static IEnumerable<Commit> GetIntermediateCommits(IRepository repo, Commit baseCommit, Commit headCommit)
        {
            if (baseCommit == null) yield break;

            if (intermediateCommitCache == null || intermediateCommitCache.LastOrDefault() != headCommit)
            {
                var filter = new CommitFilter
                {
                    IncludeReachableFrom = headCommit,
                    SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
                };

                intermediateCommitCache = repo.Commits.QueryBy(filter).ToList();
            }

            var found = false;
            foreach (var commit in intermediateCommitCache)
            {
                if (found)
                    yield return commit;

                if (commit.Sha == baseCommit.Sha)
                    found = true;
            }
        }

        private static VersionField? GetIncrementFromMessage(string message, string majorRegex, string minorRegex, string patchRegex, string none)
        {
            var key = message.GetHashCode();

            if (!VersionFieldCache.TryGetValue(key, out var version))
            {
                version = FindIncrementFromMessage(message, majorRegex, minorRegex, patchRegex, none);
                VersionFieldCache[key] = version;
            }
            return version;
        }

        private static VersionField? FindIncrementFromMessage(string message, string majorRegex, string minorRegex, string patchRegex, string noneRegex)
        {
            if(IsMatch(message, majorRegex)) return VersionField.Major;
            if(IsMatch(message, minorRegex)) return VersionField.Minor;
            if(IsMatch(message, patchRegex)) return VersionField.Patch;
            if(IsMatch(message, noneRegex)) return VersionField.None;
            return null;
        }

        private static bool IsMatch(string message, string regex)
        {
            var key = message.GetHashCode() ^ regex.GetHashCode();

            if (!MatchCache.TryGetValue(key, out var match))
            {
                match = Regex.IsMatch(message, regex, RegexOptions.IgnoreCase);
                MatchCache[key] = match;
            }
            return match;
        }

        private static readonly IDictionary<int, bool> MatchCache = new Dictionary<int, bool>();
        private static readonly IDictionary<int, VersionField?> VersionFieldCache = new Dictionary<int, VersionField?>();

        public static VersionField FindDefaultIncrementForBranch( GitVersionContext context, string branch = null )
        {
            var config = context.FullConfiguration.GetConfigForBranch(branch ?? context.CurrentBranch.NameWithoutRemote());
            if ( config?.Increment != null && config.Increment != IncrementStrategy.Inherit )
            {
                return config.Increment.Value.ToVersionField();
            }

            // Fallback to patch
            return VersionField.Patch;
        }
    }
}
