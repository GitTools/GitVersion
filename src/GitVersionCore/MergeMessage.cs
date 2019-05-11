using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitVersion
{
    public class MergeMessage
    {
        private static readonly IList<MergeMessagePattern> DefaultPatterns = new List<MergeMessagePattern>
        {
            new MergeMessagePattern("Default", @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*"),
            new MergeMessagePattern("SmartGit",  @"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*"),
            new MergeMessagePattern("BitBucketPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
            new MergeMessagePattern("GitHubPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:(?<SourceBranch>[^\s]*))(?: into (?<TargetBranch>[^\s]*))*"),
            new MergeMessagePattern("RemoteTracking", @"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*")
        };

        public MergeMessage(string mergeMessage, Config config)
        {
            if (mergeMessage == null)
                throw new NullReferenceException();

            // Concat config messages with the defaults.
            // Ensure configs are processed first.
            var allPatterns = config.MergeMessageFormats
                .Select(x => new MergeMessagePattern(x.Key, x.Value))
                .Concat(DefaultPatterns);

            foreach (var pattern in allPatterns)
            {
                var match = pattern.Format.Match(mergeMessage);
                if (match.Success)
                {
                    MatchDefinition = pattern.DefinitionName;
                    MergedBranch = match.Groups["SourceBranch"].Value;

                    if (match.Groups["TargetBranch"].Success)
                    {
                        TargetBranch = match.Groups["TargetBranch"].Value;
                    }

                    if (int.TryParse(match.Groups["PullRequestNumber"].Value, out var pullNumber))
                    {
                        PullRequestNumber = pullNumber;
                    }

                    Version = ParseVersion(MergedBranch, config.TagPrefix);

                    break;
                }
            }
        }

        public string MatchDefinition { get; }
        public string TargetBranch { get; }
        public string MergedBranch { get; } = "";
        public bool IsMergedPullRequest => PullRequestNumber != null;
        public int? PullRequestNumber { get; }
        public SemanticVersion Version { get; }

        private SemanticVersion ParseVersion(string branchName, string tagPrefix)
        {
            // Remove remotes and branch prefixes like release/ feature/ hotfix/ etc
            var toMatch = Regex.Replace(MergedBranch, @"^(\w+[-/])*", "", RegexOptions.IgnoreCase);
            toMatch = Regex.Replace(toMatch, $"^{tagPrefix}", "");
            // We don't match if the version is likely an ip (i.e starts with http://)
            var versionMatch = new Regex(@"^(?<!://)\d+\.\d+(\.*\d+)*");
            var version = versionMatch.Match(toMatch);

            if (version.Success && SemanticVersion.TryParse(version.Value, tagPrefix, out var val))
            {
                return val;
            }

            return null;
        }

        private class MergeMessagePattern
        {
            public MergeMessagePattern(string name, string format)
            {
                DefinitionName = name;
                Format = new Regex(format, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            public string DefinitionName { get; }

            public Regex Format { get; }
        }
    }
}
