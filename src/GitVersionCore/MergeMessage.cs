using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitVersion
{
    public class MergeMessage
    {
        private static readonly IList<KeyValuePair<string, Regex>> DefaultPatterns = new List<KeyValuePair<string, Regex>>
        {
            Pattern("Default", @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*"),
            Pattern("SmartGit",  @"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*"),
            Pattern("BitBucketPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
            Pattern("GitHubPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:(?<SourceBranch>[^\s]*))(?: into (?<TargetBranch>[^\s]*))*"),
            Pattern("RemoteTracking", @"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*"),
            Pattern("TfsMergeMessageEnglishUS", @"^Merge (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
            Pattern("TfsMergeMessageGermanDE",@"^Zusammengeführter PR ""(?<PullRequestNumber>\d+)""\: (?<SourceBranch>.*) mit (?<TargetBranch>.*) mergen")
        };

        public MergeMessage(string mergeMessage, Config config)
        {
            if (mergeMessage == null)
                throw new NullReferenceException();

            // Concat config messages with the defaults.
            // Ensure configs are processed first.
            var allPatterns = config.MergeMessageFormats
                .Select(x => Pattern(x.Key, x.Value))
                .Concat(DefaultPatterns);

            foreach (var pattern in allPatterns)
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

        private static KeyValuePair<string, Regex> Pattern(string name, string format)
            => new KeyValuePair<string, Regex>(name, new Regex(format, RegexOptions.IgnoreCase | RegexOptions.Compiled));
    }
}
