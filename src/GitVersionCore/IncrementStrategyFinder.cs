namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;

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
        public const string DefaultMajorTagPattern = @"\+semver-(breaking|major)";
        public const string DefaultMinorTagPattern = @"\+semver-(feature|minor)";
        public const string DefaultPatchTagPattern = @"\+semver-(fix|patch)";
        public const string DefaultNoBumpTagPattern = @"\+semver-(none|skip)";

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

            // don't increment for less than the branch config increment, if the absense of commit messages would have
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
            var majorRegex = CreateRegex(context.Configuration.MajorVersionBumpMessage ?? DefaultMajorPattern);
            var minorRegex = CreateRegex(context.Configuration.MinorVersionBumpMessage ?? DefaultMinorPattern);
            var patchRegex = CreateRegex(context.Configuration.PatchVersionBumpMessage ?? DefaultPatchPattern);
            var none = CreateRegex(context.Configuration.NoBumpMessage ?? DefaultNoBumpPattern);
            var majorTagRegex = CreateRegex(context.Configuration.MajorVersionBumpTag ?? DefaultMajorTagPattern);
            var minorTagRegex = CreateRegex(context.Configuration.MinorVersionBumpTag ?? DefaultMinorTagPattern);
            var patchTagRegex = CreateRegex(context.Configuration.PatchVersionBumpTag ?? DefaultPatchTagPattern);
            var noneTag = CreateRegex(context.Configuration.NoBumpTag ?? DefaultNoBumpTagPattern);

            var allTags = context.Repository.Tags
                .GroupBy(k => ((Commit) k.PeeledTarget()).Sha, v => v.FriendlyName)
                .ToDictionary(k => k.Key, v => v.ToArray());

            var increments = commits
                .Select(c => FindIncrementFromCommit(c.Message, allTags.ContainsKey(c.Sha) ? allTags[c.Sha] : Enumerable.Empty<string>(), majorRegex, minorRegex, patchRegex, none, majorTagRegex, minorTagRegex, patchTagRegex, noneTag))                
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

        private static VersionField? FindIncrementFromCommit(string message, IEnumerable<string> tags, Regex major, Regex minor, Regex patch, Regex none, Regex majorTag, Regex minorTag, Regex patchTag, Regex noneTag)
        {            
            if (major.IsMatch(message) || tags.Any(majorTag.IsMatch)) return VersionField.Major;
            if (minor.IsMatch(message) || tags.Any(minorTag.IsMatch)) return VersionField.Minor;
            if (patch.IsMatch(message) || tags.Any(patchTag.IsMatch)) return VersionField.Patch;
            if (none.IsMatch(message) || tags.Any(noneTag.IsMatch)) return VersionField.None;

            return null;
        }

        private static Regex CreateRegex(string pattern)
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
