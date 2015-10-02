﻿namespace GitVersion
{
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
        public const string DefaultMajorPattern = @"\+semver:\s?(breaking|major)";
        public const string DefaultMinorPattern = @"\+semver:\s?(feature|minor)";
        public const string DefaultPatchPattern = @"\+semver:\s?(fix|patch)";

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

            var majorRegex = CreateRegex(context.Configuration.MajorVersionBumpMessage ?? DefaultMajorPattern);
            var minorRegex = CreateRegex(context.Configuration.MinorVersionBumpMessage ?? DefaultMinorPattern);
            var patchRegex = CreateRegex(context.Configuration.PatchVersionBumpMessage ?? DefaultPatchPattern);

            var increments = commits
                .Select(c => FindIncrementFromMessage(c.Message, majorRegex, minorRegex, patchRegex))
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
            var filter = new CommitFilter
            {
                Since = headCommit,
                Until = baseCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            return repo.Commits.QueryBy(filter);
        }

        private static VersionField? FindIncrementFromMessage(string message, Regex major, Regex minor, Regex patch)
        {
            if (major.IsMatch(message)) return VersionField.Major;
            if (minor.IsMatch(message)) return VersionField.Minor;
            if (patch.IsMatch(message)) return VersionField.Patch;

            return null;
        }

        private static Regex CreateRegex(string pattern)
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
