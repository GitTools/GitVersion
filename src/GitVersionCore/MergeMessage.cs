using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitVersion
{
    internal class MergeMessage
    {
        private static readonly IList<KeyValuePair<string, Regex>> DefaultPatterns = new List<KeyValuePair<string, Regex>>
        {
            Pattern("Default", @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*"),
            Pattern("SmartGit",  @"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*"),
            Pattern("BitBucketPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
            Pattern("GitHubPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:(?<SourceBranch>[^\s]*))(?: into (?<TargetBranch>[^\s]*))*"),
            Pattern("RemoteTracking", @"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*")
        };

        public MergeMessage(string mergeMessage, Config config)
        {
            if (mergeMessage == null)
                throw new NullReferenceException();

            foreach(var entry in config.MergeMessageFormats)
            {
                var pattern = Pattern(entry.Key, entry.Value);
                if (ApplyPattern(mergeMessage, config.TagPrefix, pattern))
                {
                    break;
                }
            }

            foreach (var pattern in DefaultPatterns)
            {
                if (ApplyPattern(mergeMessage, config.TagPrefix, pattern))
                {
                    break;
                }
            }
        }

        public string MatchDefinition { get; private set; }
        public string TargetBranch { get; private set; }
        public string MergedBranch { get; private set; } = "";
        public bool IsMergedPullRequest => PullRequestNumber != null;
        public int? PullRequestNumber { get; private set; }
        public SemanticVersion Version { get; private set; }

        private bool ApplyPattern(string mergeMessage, string tagPrefix, KeyValuePair<string, Regex> pattern)
        {
            var match = pattern.Value.Match(mergeMessage);
            if (match.Success)
            {
                MatchDefinition = pattern.Key;
                MergedBranch = match.Groups["SourceBranch"].Value;

                if (match.Groups["TargetBranch"].Success)
                {
                    TargetBranch = match.Groups["TargetBranch"].Value;
                }

                if (int.TryParse(match.Groups["PullRequestNumber"].Value, out var pullNumber))
                {
                    PullRequestNumber = pullNumber;
                }

                Version = ParseVersion(MergedBranch, tagPrefix);

                return true;
            }

            return false;
        }

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

        private static KeyValuePair<string, Regex> Pattern(string name, string format)
            => new KeyValuePair<string, Regex>(name, new Regex(format, RegexOptions.IgnoreCase | RegexOptions.Compiled));
    }
}
